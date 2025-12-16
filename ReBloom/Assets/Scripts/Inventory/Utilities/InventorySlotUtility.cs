using UnityEngine;

public class InventorySlotUtility : MonoBehaviour
{
    public static bool TryStackItems(InventoryItemData inventory,int sourceSlot,int targetSlot)
    {
        var source = inventory.GetSlot(sourceSlot);
        var target = inventory.GetSlot(targetSlot);

        if (source?.itemID != target?.itemID)
            return false;

        var item = ItemDatabase.I.GetItem(source.itemID);
        if (item.maxCount <= 1)
            return false;

        // 스택 처리 로직...
        return true;
    }

    public static bool SplitStack(InventoryItemData inventory,int sourceSlot,int amount)
    {
        // 분할 로직...
        return true;
    }
}
