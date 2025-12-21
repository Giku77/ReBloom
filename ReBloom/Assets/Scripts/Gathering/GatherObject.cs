using UnityEngine;

public class GatherObject : MonoBehaviour, IInteractable
{
    public int gatherObjectID;

    private bool isAvailable = true;

    private GatherObjectData gatherObjectData;
    private GatherManager gatherManager;
    private GameInventory inventory;
    private PlayerEquipManager equipManager;

    private InteractionHighlight highlight;
    private string gatherName;

    public float HoldTime =>
        gatherObjectData.searchTime * equipManager.GetToolPerform();

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
        inventory = FindFirstObjectByType<GameInventory>();
        equipManager = FindFirstObjectByType<PlayerEquipManager>();
    }

    public void Initialize(GatherObjectDB db)
    {
        gatherManager = FindAnyObjectByType<GatherManager>();

        if (db.TryGet(gatherObjectID, out gatherObjectData))
        {
            gatherName =
                gatherManager.GatherObjectDB.GetTextKR(
                    gatherObjectData.objectNameId);

            if (highlight != null)
            {
                highlight.promptFormat = $"{gatherName} 조사 [E]";
                highlight.Hide();
            }
        }
    }

    public void Interact(PlayerController player)
    {
        if (!isAvailable)
            return;

        var drop = gatherManager.GetDropResult(gatherObjectID);
        if (drop?.item != null)
        {
            inventory.TryAddItemFromWorld(
                drop.item.itemID,
                drop.amount);
        }

        isAvailable = false;
    }

    public bool CanInteract()
    {
        return isAvailable;
    }
}
