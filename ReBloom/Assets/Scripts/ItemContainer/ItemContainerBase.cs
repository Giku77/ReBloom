using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ItemContainerBase : ScriptableObject, IItemContainer
{
    [SerializeField] protected List<ItemSlotData> items = new List<ItemSlotData>();
    [SerializeField] protected int maxSlots = 10;

    public IReadOnlyList<ItemSlotData> Items => items;
    public bool HasItems => items.Count > 0;
    public virtual int SlotCount => maxSlots;

    int IItemContainer.SlotCount
    {
        get => maxSlots;
        set => maxSlots = value;
    }

    public event Action OnContainerChanged;

    // 이벤트 발생 헬퍼
    protected void NotifyChanged() => OnContainerChanged?.Invoke();

    // ---- 공통 구현 ----
    /// <summary>
    /// 아이템 추가 - 부분 추가 가능
    /// </summary>
    /// <returns>실제 추가된 수량</returns>
    public virtual int AddItem(int itemID, int count)
    {
        if (count <= 0) return 0;

        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return 0;

        int originalCount = count;
        int remainingCount = count;
        int maxStack = item.maxCount;

        // 1. 기존 슬롯에 스택
        var existingSlot = items.Find(s => s.itemID == itemID);
        if (existingSlot != null && maxStack > 1)
        {
            int canAdd = Mathf.Min(maxStack - existingSlot.count, remainingCount);
            if (canAdd > 0)
            {
                existingSlot.count += canAdd;
                remainingCount -= canAdd;
            }
        }

        // 2. 새 슬롯 추가
        while (remainingCount > 0)
        {
            if (items.Count >= maxSlots)
            {
                // 공간 부족
                int addedCount = originalCount - remainingCount;
                if (addedCount > 0)
                {
                    NotifyChanged();
                }
                Debug.LogWarning($"[{GetType().Name}] 슬롯 부족! {addedCount}/{originalCount}개만 추가됨");
                return addedCount;
            }

            int toAdd = Mathf.Min(remainingCount, maxStack);
            items.Add(new ItemSlotData { itemID = itemID, count = toAdd });
            remainingCount -= toAdd;
        }

        NotifyChanged();
        return originalCount;
    }

    public virtual bool RemoveItem(int itemID, int count)
    {
        var slot = items.Find(s => s.itemID == itemID);
        if (slot == null || slot.count < count) return false;

        slot.count -= count;
        if (slot.count <= 0) items.Remove(slot);

        NotifyChanged();
        return true;
    }

    public int GetItemCount(int itemID)
    {
        var slot = items.Find(s => s.itemID == itemID);
        return slot?.count ?? 0;
    }

    public IReadOnlyList<ItemSlotData> GetAllItems() => Items;

    public virtual void Clear()
    {
        items.Clear();
        NotifyChanged();
    }

    /// <summary>
    /// 특정 수량 전송 - 부분 성공 지원
    /// </summary>
    public bool TransferTo(IItemContainer target, int itemID, int count)
    {
        if (target == null) return false;

        int availableCount = GetItemCount(itemID);
        if (availableCount < count) return false;

        int addedCount = target.AddItem(itemID, count);

        if (addedCount > 0)
        {
            RemoveItem(itemID, addedCount);
            return addedCount == count;
        }

        return false;
    }

    /// <summary>
    /// 전체 전송 - 부분 추가 지원
    /// </summary>
    public bool TransferAllTo(IItemContainer target)
    {
        if (target == null || !HasItems) return false;

        var itemsCopy = items.ToList();
        bool allTransferred = true;

        foreach (var slot in itemsCopy)
        {
            int addedCount = target.AddItem(slot.itemID, slot.count);

            if (addedCount > 0)
            {
                RemoveItem(slot.itemID, addedCount);
            }

            if (addedCount < slot.count)
            {
                allTransferred = false;
                Debug.LogWarning($"[{GetType().Name}] {slot.itemID}: {addedCount}/{slot.count}개만 전송됨");
            }
        }

        return allTransferred;
    }
}