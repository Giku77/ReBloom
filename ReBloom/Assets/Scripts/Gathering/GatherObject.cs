using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GatherObject : MonoBehaviour, IInteractable
{
    public int gatherObjectID;

    private float respawnTime;
    private float timer;
    private bool isAvailable = true;

    private GatherObjectData gatherObjectData;
    private GatherManager gatherManager;
    private GameInventory inventoryItemData;
    private InteractionHighlight highlight;
    private PlayerEquipManager playerEquipManager;
    private DayNightCycle dayNightCycle;

    private int objectNameID;
    private string gatherName;

    private string gatherAvailableText = "조사 시작 [E]";
    private string gatherNotAvailableText = "조사 불가";

    private NetworkGatherObject netGather;

    [Header("채집 후 파괴 설정")]
    public bool isDestroyObject = false;

    [Header("튜토리얼 후 삭제")]
    public bool isTutorialObject = false;

    [Header("망치 필요 여부")]
    public bool requireHammer = false;

    [Header("군수공장 펜스")]
    [SerializeField] private GameObject fence;
    [SerializeField] private Transform fenceCamPos;  
    [SerializeField] private Transform fenceLookAt;

    [Header("Save Key (고정)")]
    [SerializeField] private string persistentId; // 예: bridge_bus_1, fence_gate_1
    public string SaveKey => $"gather_destroyed:{persistentId}";


    public float HoldTime
    {
        get
        {
            if (gatherObjectData == null) return 1f;

            int searchType = GetCurrentSearchType();

            if (searchType == 0) return float.MaxValue;
            if (searchType == 1) return gatherObjectData.searchTime;
            if (searchType == 2)
            {
                return gatherObjectData.searchTime * playerEquipManager.GetToolPerform();
            }

            return gatherObjectData.searchTime;
        }
    }

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
        playerEquipManager = FindFirstObjectByType<PlayerEquipManager>();
        inventoryItemData = FindFirstObjectByType<GameInventory>();
        dayNightCycle = FindFirstObjectByType<DayNightCycle>();
        netGather = GetComponent<NetworkGatherObject>();
    }

    private void Update()
    {
        if (netGather != null && netGather.IsSpawned)
        {
            return;
        }
        if (!isAvailable)
        {
            timer += Time.deltaTime;

            if (timer >= respawnTime)
            {
                isAvailable = true;
                timer = respawnTime;

                if (highlight != null)
                {
                    //highlight.ShowHighlightOnly();
                    UpdatePromptText();
                }
            }
        }
    }

    private void OnEnable()
    {
        if (isDestroyObject)
            CheckDestroyedDeferred().Forget();

        if (isTutorialObject && QuestManager.I != null && QuestManager.I.FirstQuestCompleted)
        {
            DestroyAfterTutorialClear();
            return;
        }
        QuestManager.OnFirstQuestCompleted += DestroyAfterTutorialClear;
    }

    private void OnDisable()
    {
        QuestManager.OnFirstQuestCompleted -= DestroyAfterTutorialClear;
    }

    private async UniTaskVoid CheckDestroyedDeferred()
    {
        await UniTask.WaitUntil(() => SaveManager.I != null && SaveManager.I.HasLoadedOnce);
        await UniTask.DelayFrame(1);

        if (!this || !gameObject) return;

        if (DestroyedObjectRegistry.I != null && DestroyedObjectRegistry.I.IsDestroyed(SaveKey))
        {
            if (fence != null) fence.SetActive(false);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public float GetRespawnSecondsSafe()
    {
        return respawnTime > 0f ? respawnTime : 3f;
    }

    public void RefreshFromNetwork()
    {
        if (highlight == null) return;
        if (netGather == null || !netGather.IsSpawned) return;

        highlight.isPermanent = false;
        highlight.promptFormat = GetCurrentPromptText();
    }

    public void Interact(PlayerController player)
    {
        if (player == null) return;
        if (netGather != null && netGather.IsSpawned)
        {
            if (!netGather.IsAvailableNow())
            {
                ToastMessageUI.Instance?.Show("아직 재생성 중입니다.");
                return;
            }

            string failReason = GetInteractionFailReason();
            if (!string.IsNullOrEmpty(failReason))
            {
                ToastMessageUI.Instance?.Show(failReason);
                return;
            }

            netGather.TryRequestGather(player);
            return;
        }

        if (!isAvailable) return;

        //if (requireHammer && !HasHammerEquipped())
        //{
        //    ToastMessageUI.Instance?.Show("망치가 필요합니다.");
        //    Debug.Log("[GatherObject] 망치가 장착되지 않음");
        //    return;
        //}

        string failReason2 = GetInteractionFailReason();
        if (!string.IsNullOrEmpty(failReason2))
        {
            ToastMessageUI.Instance?.Show(failReason2);
            Debug.Log($"[GatherObject] {failReason2}");
            return;
        }

        Debug.Log($"[GatherObject] 상호작용 시작 - gatherObjectID: {gatherObjectID}");

        bool isNight = dayNightCycle != null && dayNightCycle.IsNightTime();

        //var drops = gatherManager.GetDropResult(gatherObjectID);

        var drops = gatherManager.GetDropResult(gatherObjectID, isNight);

        if (drops != null && drops.item != null)
        {
            if (inventoryItemData.TryAddItemFromWorld(drops.item.itemID, drops.amount))
            {
                Debug.Log($"[GatherObject] {drops.item.itemName} {drops.amount}개 획득");
                isAvailable = false;
                timer = 0f;

                // 1) 상호작용 퀘스트 진행
                NetworkQuestManager.I?.ReportInteract(gatherObjectID, 1);

                // 2) 수집 퀘스트도 공용으로 올리고 싶으면 같이
                NetworkQuestManager.I?.ReportCollect(drops.item.itemID, drops.amount);
            }
            else
            {
                return;
            }
        }
        else
        {
            Debug.Log("[GatherObject] 보관 아이템이 null입니다.");
        }

        //isAvailable = false;
        //timer = 0f;

        //QuestManager.I?.NotifyInteracted(gatherObjectID);

        if (isDestroyObject)
        {
            Debug.Log($"[GatherObject] 오브젝트 제거: {gatherName}");
            DestroyedObjectRegistry.I?.MarkDestroyed(SaveKey);

            if (gatherObjectID == 910019)
            {
                ToastMessageUI.Instance?.Show("다리를 막는 버스를 부쉈습니다.");
            }

           if (gatherObjectID == 910020 && fence != null)
            {
                //QuestManager.I?.NotifyInteracted(gatherObjectID);
                ToastMessageUI.Instance?.Show("군수공장의 문이 열렸습니다.");

                var cam = Camera.main ? Camera.main.GetComponent<ThirdPersonCamera>() : null;

                if (cam != null && fenceCamPos != null)
                {
                    Vector3 lookAt = fenceLookAt != null
                        ? fenceLookAt.position
                        : (fence.transform.position + Vector3.up * 1.2f);

                    Vector3 camPos = fenceCamPos.position;

                    cam.PlayFocusSequenceUniTask(
                        focusLookAtWorld: lookAt,
                        cameraPosWorld: camPos,
                        blendIn: 0.35f,
                        hold: 0.7f,
                        blendOut: 0.35f,
                        onMidAction: () =>
                        {
                            fence.SetActive(false);   
                        }
                    );
                }
                else
                {
                    // 포인트/카메라 없으면 그냥 제거
                    fence.SetActive(false);
                }
            }

            if (highlight != null)
            {
                highlight.Hide();
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            if (highlight != null)
            {
                highlight.isPermanent = false;
               // highlight.Hide();
                highlight.promptFormat = gatherNotAvailableText;
                highlight.ShowPrompt();
            }
        }
    }

    public void Initialize(GatherObjectDB db)
    {
        gatherManager = FindAnyObjectByType<GatherManager>();

        if (db.TryGet(gatherObjectID, out gatherObjectData))
        {
            respawnTime = gatherObjectData.respawnTime;
            timer = respawnTime;

            objectNameID = gatherObjectData.objectNameId;
            gatherName = gatherManager.GatherObjectDB.GetTextKR(objectNameID);

            UpdatePromptText();

            if (highlight != null)
            {
                //highlight.ShowHighlightOnly();
                highlight.promptFormat = gatherAvailableText;
            }
        }
    }

    private void UpdatePromptText()
    {
        gatherAvailableText = $"{gatherName} 조사 [E]";
        gatherNotAvailableText = $"{gatherName} 조사 완료";
    }

    //private bool HasHammerEquipped()
    //{
    //    var equipData = playerEquipManager.GetComponent<PlayerEquipData>();

    //    if (equipData == null || equipData.currentToolEquip == null)
    //        return false;

    //    return equipData.currentToolEquip.toolCategory == ToolCategory.Hammer;
    //}

    public bool CanInteract()
    {
        if (netGather != null && netGather.IsSpawned)
        {
            if (!netGather.IsAvailableNow()) return false;
            return string.IsNullOrEmpty(GetInteractionFailReason());
        }

        if (!isAvailable) return false;
        return string.IsNullOrEmpty(GetInteractionFailReason());
    }


    public void DestroyAfterTutorialClear()
    {
        if (!isTutorialObject) return;

        if (netGather != null && netGather.IsSpawned)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netGather.ServerDisablePermanently();
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        if (highlight != null) highlight.Hide();
        Destroy(gameObject);
    }

    public string GetCurrentPromptText()
    {
        if (netGather != null && netGather.IsSpawned)
        {
            if (netGather.IsAvailableNow())
                return gatherAvailableText;

            int sec = Mathf.CeilToInt(netGather.GetCooldownRemaining());
            return sec > 0
                ? $"{gatherName} 조사 완료 ({sec}s)"
                : $"{gatherName} 조사 완료";
        }

        return isAvailable ? gatherAvailableText : gatherNotAvailableText;
    }

    private int GetCurrentSearchType()
    {
        if (gatherObjectData == null) return 0;

        var equipData = playerEquipManager?.GetComponent<PlayerEquipData>();

        if (equipData == null || equipData.currentToolEquip == null)
        {
            return gatherObjectData.handSearchType;
        }

        var tool = equipData.currentToolEquip;

        switch (tool.toolCategory)
        {
            case ToolCategory.Hand:
                return gatherObjectData.handSearchType;
            case ToolCategory.Shovel:
                return gatherObjectData.shovelSearchType;
            case ToolCategory.Hammer:
                return gatherObjectData.hammerSearchType;
            default:
                return 0;
        }
    }

    //private string GetRequiredToolText()
    //{
    //    if (gatherObjectData == null) return "";

    //    bool handOk = gatherObjectData.handSearchType > 0;
    //    bool shovelOk = gatherObjectData.shovelSearchType > 0;
    //    bool hammerOk = gatherObjectData.hammerSearchType > 0;

    //    if (handOk && !shovelOk && !hammerOk)
    //        return "(맨손)";
    //    else if (!handOk && shovelOk && !hammerOk)
    //        return "(삽 필요)";
    //    else if (!handOk && !shovelOk && hammerOk)
    //        return "(망치 필요)";
    //    else if ((handOk ? 1 : 0) + (shovelOk ? 1 : 0) + (hammerOk ? 1 : 0) > 1)
    //        return "";

    //    return "";
    //}

    private string GetInteractionFailReason()
    {
        if (gatherObjectData == null) return "데이터 없음";

        if (gatherObjectData.nightOnly == 1)
        {
            if (dayNightCycle == null || !dayNightCycle.IsNightTime())
            {
                return "밤에만 채집할 수 있습니다.";
            }
        }

        var equipData = playerEquipManager?.GetComponent<PlayerEquipData>();

        Debug.Log($"[GatherObject] 현재 장비 확인 - equipData null: {equipData == null}, tool null: {equipData?.currentToolEquip == null}");


        if (equipData == null || equipData.currentToolEquip == null)
        {
            Debug.Log($"[GatherObject] 맨손 체크 - handSearchType: {gatherObjectData.handSearchType}");

            if (gatherObjectData.handSearchType == 0)
            {
                return GetToolRequirementMessage();
            }
            return null;
        }

        var tool = equipData.currentToolEquip;

        switch (tool.toolCategory)
        {
            case ToolCategory.Hand:
                if (gatherObjectData.handSearchType == 0)
                    return GetToolRequirementMessage();
                break;

            case ToolCategory.Shovel:
                if (gatherObjectData.shovelSearchType == 0)
                    return GetToolRequirementMessage();
                break;

            case ToolCategory.Hammer:
                if (gatherObjectData.hammerSearchType == 0)
                    return GetToolRequirementMessage();
                break;
        }

        return null;
    }

    private string GetToolRequirementMessage()
    {
        if (gatherObjectData == null) return "도구가 필요합니다.";

        bool handOk = gatherObjectData.handSearchType > 0;
        bool shovelOk = gatherObjectData.shovelSearchType > 0;
        bool hammerOk = gatherObjectData.hammerSearchType > 0;

        if (handOk && !shovelOk && !hammerOk)
            return "맨손으로만 채집할 수 있습니다.";

        if (shovelOk && hammerOk)
            return "도구가 필요합니다.";
        else if (shovelOk)
            return "삽이 필요합니다.";
        else if (hammerOk)
            return "망치가 필요합니다.";
        else if (handOk)
            return null;

        return "채집할 수 없습니다.";
    }

    public string GetCannotInteractMessage()
    {
        if (netGather != null && netGather.IsSpawned)
        {
            if (!netGather.IsAvailableNow())
                return "아직 재생성 중입니다.";
            return GetInteractionFailReason();
        }

        if (!isAvailable) return "아직 재생성 중입니다.";
        return GetInteractionFailReason();
    }
}
