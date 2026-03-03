using UnityEngine;

public class WorldItem : MonoBehaviour, IInteractable
{
    private ItemBase itemData;
    private int quantity = 1;
    private bool isPersistent;

    private PooledItem pooledItem;

    public float HoldTime => 0f;

    private void Awake()
    {
        pooledItem = GetComponent<PooledItem>();
    }

    public void Initialize(ItemBase item, bool isDropped = true)
    {
        itemData = item;
        isPersistent = !isDropped;
        RefreshHighlight();
    }

    public void SetQuantity(int amount)
    {
        quantity = Mathf.Max(1, amount);
        RefreshHighlight();
    }

    public void SetPersistent(bool persistent)
    {
        isPersistent = persistent;
    }

    public ItemBase GetItemData() => itemData;
    public int GetQuantity() => quantity;

    private void RefreshHighlight()
    {
        if (itemData == null) return;

        var highlight = GetComponent<InteractionHighlight>();
        if (highlight != null)
        {
            highlight.promptFormat = quantity > 1
                ? $"{itemData.itemName} x{quantity} 줍기 [E]"
                : $"{itemData.itemName} 줍기 [E]";
            highlight.isPermanent = true;
        }
    }

    public void Interact(PlayerController player)
    {
        // 로컬/싱글용 fallback
        var net = GetComponent<NetworkWorldItem>();
        if (net != null && net.IsSpawned)
        {
            net.TryRequestPickup(player);
            return;
        }

        LocalPickup(player);
    }

    private bool LocalPickup(PlayerController player)
    {
        var inventoryData = player.Inventory;
        if (inventoryData == null || itemData == null) return false;

        int addedCount = inventoryData.AddItemFromWorld(itemData.itemID, quantity);
        if (addedCount <= 0) return false;

        if (addedCount < quantity)
        {
            quantity -= addedCount;
            RefreshHighlight();
            return true;
        }

        if (pooledItem != null) pooledItem.ReturnToPool();
        else Destroy(gameObject);

        return true;
    }

    public bool CanInteract() => itemData != null;

    public void ResetItem()
    {
        itemData = null;
        quantity = 1;
        isPersistent = false;

        var highlight = GetComponent<InteractionHighlight>();
        if (highlight != null)
        {
            highlight.Hide();
            highlight.isPermanent = false;
        }
    }
}