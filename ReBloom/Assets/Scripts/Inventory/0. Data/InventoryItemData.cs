using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "ReBloom/Inventory/GameInventory Data")]
public class InventoryItemData : ScriptableObject, IItemContainer
{
    [Header("Settings")]
    [SerializeField] private int inventoryTier = 0;
    [SerializeField] private int MaxTier = 3;
    
    [SerializeField] private ItemSlotData[] slots;

    // ---- 속성 ----
    public int SlotCount => GetSlotCountForTier(inventoryTier);

    public bool HasLockedSlots => inventoryTier < 3;
    public int LockedSlotCount => inventoryTier < 3 ? 5 : 0;

    public IReadOnlyList<ItemSlotData> Items => GetValidSlots();

    public bool HasItems => slots != null;
    public bool HasItem(int itemID, int count)
    {
        return GetItemCount(itemID) >= count;
    }

    // ---- 이벤트 (데이터 변경만) ----
    public event Action OnContainerChanged;

    // ---- IItemContainer 핵심 구현 ----
    public int TryAddItem(int itemID, int count)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null) return 0;

        int added = 0;
        int remaining = count;

        // 1. 기존 스택에 추가
        added += TryStackExisting(itemID, ref remaining, item.maxCount);

        // 2. 빈 슬롯에 추가
        added += TryAddToEmpty(itemID, ref remaining, item.maxCount);

        if (added > 0) OnContainerChanged?.Invoke();

        return added;
    }

    public bool TryRemoveItem(int itemID, int count)
    {
        if (GetItemCount(itemID) < count) return false;

        int remaining = count;
        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i]?.itemID == itemID)
            {
                int remove = Mathf.Min(slots[i].count, remaining);
                slots[i].count -= remove;

                if (slots[i].count <= 0)
                    slots[i] = null;

                remaining -= remove;
            }
        }

        OnContainerChanged?.Invoke();
        return true;
    }
    public bool RemoveAtSlot(int slotIndex, int count = -1)
    {
        if (!IsValidSlotIndex(slotIndex) || slots[slotIndex] == null)
            return false;

        if (count == -1 || count >= slots[slotIndex].count)
            slots[slotIndex] = null;
        else
            slots[slotIndex].count -= count;

        OnContainerChanged?.Invoke();
        return true;
    }

    public int GetItemCount(int itemID)
    {
        return slots.Where(s => s?.itemID == itemID).Sum(s => s.count);
    }

    public void Clear()
    {
        Array.Clear(slots, 0, slots.Length);
        OnContainerChanged?.Invoke();
    }

    // ---- 티어 시스템 ----
    public bool CanExpandWithChip(int chipTier)
    {
        // 다음 단계가 아니면 실패
        return inventoryTier + 1 == chipTier && inventoryTier < MaxTier;
    }

    public bool ExpandWithChip(int chipTier)
    {
        if (!CanExpandWithChip(chipTier))
            return false;

        inventoryTier++;
        OnContainerChanged?.Invoke();
        return true;
    }

    // ---- 슬롯 직접 조작 (UI용) ----
    public ItemSlotData GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return null;
        return slots[index];
    }

    public bool SwapSlots(int from, int to)
    {
        if (!IsValidSlotIndex(from) || !IsValidSlotIndex(to))
            return false;

        (slots[from], slots[to]) = (slots[to], slots[from]);
        OnContainerChanged?.Invoke();
        return true;
    }

    // ---- Private Helpers ----
    private int GetSlotCountForTier(int tier) => tier switch
    {
        0 => 10,
        1 => 20,
        2 => 30,
        3 => 40,
        _ => 10
    };

    private bool IsValidSlotIndex(int index)
        => index >= 0 && index < SlotCount;

    private IReadOnlyList<ItemSlotData> GetValidSlots()
    {
        var result = new List<ItemSlotData>();
        for (int i = 0; i < SlotCount; i++)
        {
            result.Add(slots[i] ?? new ItemSlotData { itemID = 0, count = 0 });
        }
        return result.AsReadOnly();
    }

    public ItemSlotData[] GetAllSlots()
    {
        var result = new ItemSlotData[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            result[i] = slots[i];
        }
        return result;
    }

    public bool IsEmptySlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;
        return slots[slotIndex] == null || slots[slotIndex].itemID <= 0;
    }

    /// <summary>
    /// 기존 스택에 추가 시도
    /// </summary>
    private int TryStackExisting(int itemID, ref int remaining, int maxStack)
    {
        if (maxStack <= 1 || remaining <= 0)
            return 0;

        int totalAdded = 0;

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            // 같은 아이템이고 스택 공간이 있는 슬롯 찾기
            if (slots[i] != null && slots[i].itemID == itemID)
            {
                int currentCount = slots[i].count;
                int availableSpace = maxStack - currentCount;

                if (availableSpace > 0)
                {
                    int toAdd = Mathf.Min(availableSpace, remaining);
                    slots[i].count += toAdd;
                    totalAdded += toAdd;
                    remaining -= toAdd;
                }
            }
        }

        return totalAdded;
    }

    /// <summary>
    /// 빈 슬롯에 새로 추가
    /// </summary>
    private int TryAddToEmpty(int itemID, ref int remaining, int maxStack)
    {
        if (remaining <= 0) return 0;

        int totalAdded = 0;

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            // 빈 슬롯 찾기
            if (slots[i] == null || slots[i].itemID <= 0)
            {
                int toAdd = Mathf.Min(remaining, maxStack);

                slots[i] = new ItemSlotData
                {
                    itemID = itemID,
                    count = toAdd
                };

                totalAdded += toAdd;
                remaining -= toAdd;
            }
        }
        return totalAdded;
    }

    public int AddItemWithOverflow(int itemID, int amount, out int overflow)
    {
        int added = TryAddItem(itemID, amount);
        overflow = amount - added;
        return added;
    }

    public void Initialize()
    {
        // 슬롯 배열만 초기화
        if (slots == null || slots.Length != 40)
        {
            slots = new ItemSlotData[40];
        }

        OnContainerChanged?.Invoke();  // UI 초기 갱신용
    }
    private void OnEnable()
    {
        if (slots == null || slots.Length != 40)
            slots = new ItemSlotData[40];
    }
}