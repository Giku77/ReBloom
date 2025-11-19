using UnityEngine;

public class GatherObject : MonoBehaviour, IInteractable
{
    public int gatherObjectID;

    private GatherObjectData gatherObjectData;

    private GatherManager gatherManager;

    [SerializeField] private InventoryItemData inventoryItemData;

    public float HoldTime => gatherObjectData.searchTime;

    public void Interact(PlayerController player)
    {
        if (player == null)
            return;

        Debug.Log($"[GatherObject] 상호작용 시작 - gatherObjectID: {gatherObjectID}");

        var drops = gatherManager.GetDropResult(gatherObjectID);

        if (drops == null)
        {
            Debug.Log("[GatherObject] 보관 아이템이 null입니다.");
            return;
        }

        inventoryItemData.AddItem(drops.itemID, 1);
        Debug.Log($"[GatherObject] {drops.itemName} 획득");

        Destroy(gameObject);
    }

    public void Initialize(GatherObjectDB db)
    {
        gatherManager = FindAnyObjectByType<GatherManager>();

        if (db.TryGet(gatherObjectID, out gatherObjectData))
        {
            Debug.Log($"채집 오브젝트 초기화 {gatherObjectData.objectNameId}");
        }
    }
}
