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
    bool HasItems { get; }
    int SlotCount { get; set; }
    // 이벤트
    event Action OnContainerChanged;

    // 핵심 기능
    bool AddItem(int itemID, int count);
    bool RemoveItem(int itemID, int count);
    int GetItemCount(int itemID);
    void Clear();

    // 컨테이너 간 이동
    bool TransferTo(IItemContainer target, int itemID, int count);
    bool TransferAllTo(IItemContainer target);
}