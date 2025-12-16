using System.Linq;
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
        if (from == null || to == null || !from.HasItems)
        {
            Debug.Log($"[TransferAll] 전송 취소 - from null: {from == null}, to null: {to == null}, HasItems: {from?.HasItems}");
            return false;
        }

        int transferredCount = 0;
        int failedCount = 0;

        // ToList()로 복사본 생성
        foreach (var slot in from.Items.ToList())
        {
            // 유효한 아이템만 처리
            if (slot != null && slot.itemID > 0 && slot.count > 0)
            {
                Debug.Log($"[TransferAll] 전송 시도: ID={slot.itemID}, Count={slot.count}");

                int added = to.TryAddItem(slot.itemID, slot.count);

                if (added > 0)
                {
                    bool removed = from.TryRemoveItem(slot.itemID, added);
                    if (removed)
                    {
                        transferredCount++;
                        Debug.Log($"  → 성공: {added}개 전송");
                    }
                    else
                    {
                        Debug.LogWarning($"  → 제거 실패!");
                    }
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"  → 추가 실패!");
                }
            }
        }

        Debug.Log($"[TransferAll] 완료 - 성공: {transferredCount}, 실패: {failedCount}");
        return failedCount == 0;
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
