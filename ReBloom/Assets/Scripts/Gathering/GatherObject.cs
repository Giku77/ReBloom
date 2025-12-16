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
                    highlight.promptFormat = gatherAvailableText;
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

        Debug.Log($"[GatherObject] 상호작용 시작 - gatherObjectID: {gatherObjectID}");
        var drops = gatherManager.GetDropResult(gatherObjectID);

        if (drops == null || drops.item == null)
        {
            Debug.Log("[GatherObject] 보관 아이템이 null입니다.");
            return;
        }

        inventoryItemData.TryAddItemFromWorld(drops.item.itemID, drops.amount); //실패 시 월드 드롭하려면: AddItemFromWorld()
        Debug.Log($"[GatherObject] {drops.item.itemName} {drops.amount}개 획득");

        isAvailable = false;
        timer = 0;
        if (highlight != null)
        {
            highlight.isPermanent = false;
            highlight.Hide();
            highlight.promptFormat = gatherNotAvailableText;
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

            gatherAvailableText = $"{gatherName} 조사 [E]";
            gatherNotAvailableText = $"{gatherName} 조사 완료";

            if (highlight != null)
            {
                highlight.ShowHighlightOnly();
                highlight.promptFormat = gatherAvailableText;
            }
        }
    }

    public bool CanInteract()
    { 
        return isAvailable;
    }
}
