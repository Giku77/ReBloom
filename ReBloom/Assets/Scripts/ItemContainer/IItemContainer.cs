// ========================================
// 모든 아이템 컨테이너가 지켜야 함
// ReBloom: 인벤토리 / 상자 / 시체박스 / 건축물
// ========================================
using System;
using System.Collections.Generic;

public interface IItemContainer
{
    // 읽기 전용 속성
    IReadOnlyList<ItemSlotData> Items { get; }
    int SlotCount { get; }
    bool HasItems { get; }

    // 이벤트
    event Action OnContainerChanged;

    // 핵심 메서드
    int TryAddItem(int itemID, int count);
    bool TryRemoveItem(int itemID, int count);
    int GetItemCount(int itemID);
    void Clear();
}