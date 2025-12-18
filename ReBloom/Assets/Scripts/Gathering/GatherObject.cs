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
    private int objectNameID;
    private string gatherName;


    private string gatherAvailableText = $"조사 시작 [E]";
    private string gatherNotAvailableText = "조사 불가";

    [Header("채집 후 파괴 설정")]
    public bool isDestroyObject = false;

    [Header("망치 필요 여부")]
    public bool requireHammer = false;

    public float HoldTime => gatherObjectData.searchTime * playerEquipManager.GetToolPerform();

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();

        playerEquipManager = FindFirstObjectByType<PlayerEquipManager>();
        inventoryItemData = GameObject.FindFirstObjectByType<GameInventory>();
    }

    private void Update()
    {
        if (!isAvailable)
        {
            timer += Time.deltaTime;
            if (timer >= respawnTime)
            {
                isAvailable = true;
                timer = respawnTime;

                if (highlight != null)
                {
                    highlight.ShowHighlightOnly();
                    UpdatePromptText();
                }

            }
        }
    }

    public void Interact(PlayerController player)
    {
        if (player == null)
            return;
        if (!isAvailable)
            return;

        if (requireHammer && !HasHammerEquipped())
        {
            ToastMessageUI.Instance?.Show("망치가 필요합니다.");
            Debug.Log("[GatherObject] 망치가 장착되지 않음");
            return;
        }

        Debug.Log($"[GatherObject] 상호작용 시작 - gatherObjectID: {gatherObjectID}");

        var drops = gatherManager.GetDropResult(gatherObjectID);

        if (drops != null && drops.item != null)
        {
            inventoryItemData.TryAddItemFromWorld(drops.item.itemID, drops.amount);
            Debug.Log($"[GatherObject] {drops.item.itemName} {drops.amount}개 획득");
        }
        else
        {
            Debug.Log("[GatherObject] 보관 아이템이 null입니다.");
        }

        isAvailable = false;
        timer = 0;

        if (isDestroyObject)
        {
            Debug.Log($"[GatherObject] 오브젝트 제거: {gatherName}");
            if (gatherObjectID == 910019)
                ToastMessageUI.Instance?.Show("다리를 막는 버스를 부쉈습니다.");
            if (highlight != null)
            {
                highlight.Hide();
            }
            Destroy(gameObject);
        }
        else
        {
            if (highlight != null)
            {
                highlight.isPermanent = false;
                highlight.Hide();
                highlight.promptFormat = gatherNotAvailableText;
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

            Debug.Log($"채집 오브젝트 초기화 {gatherObjectData.objectNameId}");

            objectNameID = gatherObjectData.objectNameId;
            gatherName = gatherManager.GatherObjectDB.GetTextKR(objectNameID);

            UpdatePromptText();

            if (highlight != null)
                {
                    highlight.ShowHighlightOnly();
                    highlight.promptFormat = gatherAvailableText;
                }
        }
    }
    private void UpdatePromptText()
    {
        if (requireHammer)
        {
            gatherAvailableText = $"{gatherName} 파괴 (망치 필요) [E]";
        }
        else
        {
            gatherAvailableText = $"{gatherName} 조사 [E]";
        }
        gatherNotAvailableText = $"{gatherName} 조사 완료";
    }

    private bool HasHammerEquipped()
    {
        var equipData = playerEquipManager.GetComponent<PlayerEquipData>();
        if (equipData == null || equipData.currentToolEquip == null)
            return false;

        return equipData.currentToolEquip.toolCategory == ToolCategory.Hammer;
    }

    public bool CanInteract()
    {
        if (!isAvailable)
            return false;

        if (requireHammer && !HasHammerEquipped())
            return false;

        return true;
    }
}
