using UnityEngine;

public class ItemTransferUtility
{
    /// <summary>
    /// 아이템 일부/전체 이동
    /// </summary>
    /// <returns>요청 수량 전부 이동했으면 true</returns>
    public static bool TransferItem(IItemContainer from, IItemContainer to, int itemID, int count)
    {
        if (from.GetItemCount(itemID) < count) return false;

        var added = to.TryAddItem(itemID, count);
        if (added <= 0) return false;

        if (added > 0)
        {
            from.TryRemoveItem(itemID, added);
        }

        return added == count;
    }
    public static bool TransferAll(IItemContainer from, IItemContainer to)
    {
        if (!from.HasItems)
            return false;

        bool allSuccess = true;

        foreach (var slot in from.Items)
        {
            if (slot.itemID <= 0) continue;

            if (!TransferItem(from, to, slot.itemID, slot.count))
                allSuccess = false;
        }

        return allSuccess;
    }

    // 드래그 앤 드롭도 통합
    public static void HandleDragDrop(IItemContainer source, IItemContainer target, int slotIndex)
    {
        // 인벤토리 ↔ 창고
        // 인벤토리 ↔ 시체박스  
        // 창고 ↔ 시체박스
        // 모두 같은 로직
    }
}
