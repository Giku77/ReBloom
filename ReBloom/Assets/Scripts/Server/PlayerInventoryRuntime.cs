using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryRuntime : MonoBehaviour
{
    [Header("Template")]
    [SerializeField] private InventoryItemData inventoryTemplate;

    private InventoryItemData runtimeData;

    public InventoryItemData Data => runtimeData;
    public bool HasItems => runtimeData != null && runtimeData.HasItems;
    public int SlotCount => runtimeData != null ? runtimeData.SlotCount : 0;
    public IReadOnlyList<ItemSlotData> Items => runtimeData != null ? runtimeData.Items : null;

    public event Action OnChanged;

    private void Awake()
    {
        if (inventoryTemplate == null)
        {
            Debug.LogError("[PlayerInventoryRuntime] inventoryTemplate이 없습니다!");
            return;
        }

        // 플레이어마다 런타임 복제본 생성
        runtimeData = Instantiate(inventoryTemplate);
        runtimeData.name = $"{inventoryTemplate.name}_Runtime_{gameObject.name}";
        runtimeData.Initialize();

        runtimeData.OnContainerChanged += HandleChanged;
    }

    private void OnDestroy()
    {
        if (runtimeData != null)
        {
            runtimeData.OnContainerChanged -= HandleChanged;
            Destroy(runtimeData);
        }
    }

    private void HandleChanged()
    {
        OnChanged?.Invoke();
    }

    public int GetItemCount(int itemID)
    {
        return runtimeData != null ? runtimeData.GetItemCount(itemID) : 0;
    }

    public bool HasItem(int itemID, int count)
    {
        return runtimeData != null && runtimeData.HasItem(itemID, count);
    }

    public int AddItemFromWorld(int itemID, int count)
    {
        if (runtimeData == null) return 0;
        return runtimeData.TryAddItem(itemID, count);
    }

    public bool TryAddItemFromWorld(int itemID, int count)
    {
        if (runtimeData == null) return false;
        int added = runtimeData.TryAddItem(itemID, count);
        return added == count;
    }

    public bool TryRemoveItem(int itemID, int count)
    {
        if (runtimeData == null) return false;
        return runtimeData.TryRemoveItem(itemID, count);
    }

    public bool SwapSlots(int fromIndex, int toIndex)
    {
        if (runtimeData == null) return false;
        return runtimeData.SwapSlots(fromIndex, toIndex);
    }

    public void Clear()
    {
        runtimeData?.Clear();
    }

    public bool TryExpandWithChip(int tier)
    {
        return runtimeData != null && runtimeData.ExpandWithChip(tier);
    }

    public bool IsEmptySlot(int slotIndex)
    {
        return runtimeData != null && runtimeData.IsEmptySlot(slotIndex);
    }

    public ItemSlotData[] GetAllSlots()
    {
        return runtimeData != null ? runtimeData.GetAllSlots() : Array.Empty<ItemSlotData>();
    }

    public int TryAddItem(int itemID, int count)
    {
        if (runtimeData == null) return 0;
        return runtimeData.TryAddItem(itemID, count);
    }

    public int AddItemWithOverflow(int itemID, int amount, out int overflow)
    {
        overflow = amount;

        if (runtimeData == null)
            return 0;

        return runtimeData.AddItemWithOverflow(itemID, amount, out overflow);
    }
}