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

    [SerializeField] private InventoryItemData inventoryItemData;

    private InteractionHighlight highlight;

    public float HoldTime => gatherObjectData.searchTime;

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
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
                    highlight.Show();
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

        if (drops == null)
        {
            Debug.Log("[GatherObject] 보관 아이템이 null입니다.");
            return;
        }

        inventoryItemData.AddItem(drops.itemID, 1);
        Debug.Log($"[GatherObject] {drops.itemName} 획득");

        isAvailable = false;
        timer = 0;

        if (highlight != null)
            highlight.Hide();
    }

    public void Initialize(GatherObjectDB db)
    {
        gatherManager = FindAnyObjectByType<GatherManager>();

        if (db.TryGet(gatherObjectID, out gatherObjectData))
        {
            respawnTime = gatherObjectData.respawnTime;

            timer = respawnTime;

            Debug.Log($"채집 오브젝트 초기화 {gatherObjectData.objectNameId}");

            if (highlight != null)
                highlight.Show();
        }
    }

    public bool CanInteract()
    { 
        return isAvailable;
    }
}
