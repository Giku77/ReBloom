using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "ReBloom/Inventory/GameInventory Data")]
public class InventoryItemData : ScriptableObject, IItemContainer
{
    [Header("Settings")]
    [SerializeField] private int inventoryTier = 0;

    public int InventoryTier
    {
        get => inventoryTier;
        private set => inventoryTier = Mathf.Clamp(value, 0, 3);
    }

    public int SlotCount
    {
        get => inventoryTier switch
        {
            0 => 10,
            1 => 20,
            2 => 30,
            3 => 40,
            _ => 10
        };
        set
        {
            if (value <= 10) InventoryTier = 0;
            else if (value <= 20) InventoryTier = 1;
            else if (value <= 30) InventoryTier = 2;
            else InventoryTier = 3;
        }
    }

    // Dictionary -> List로 변경
    [SerializeField] private List<ItemSlotData> _slots = new List<ItemSlotData>();

    // ---- 이벤트 ----
    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;
    public event Action<string> OnMessage;
    public event Action<ItemBase, int> OnItemToastMessage;
    public event Action<string, Color> OnWarningMessage;

    // ---- IItemContainer 구현 ----
    public event Action OnContainerChanged
    {
        add => OnInventoryChanged += value;
        remove => OnInventoryChanged -= value;
    }

    public IReadOnlyList<ItemSlotData> Items => _slots.AsReadOnly();

    public bool HasItems => _slots.Count > 0;

    // ---- 메서드 ----

    /// <summary>
    /// 아이템 추가 (스택 로직 포함)
    /// </summary>
    public bool AddItem(int itemID, int count)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[InventoryData] 아이템 ID {itemID}를 찾을 수 없습니다!");
            return false;
        }

        int maxCount = item.maxCount;
        int remainingCount = count;

        // 1. 기존 슬롯에 스택 가능한지 확인 (maxCount > 1인 경우만)
        if (maxCount > 1)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].itemID == itemID)
                {
                    int currentCount = _slots[i].count;
                    int canStack = Mathf.Min(maxCount - currentCount, remainingCount);

                    if (canStack > 0)
                    {
                        _slots[i] = new ItemSlotData
                        {
                            itemID = itemID,
                            count = currentCount + canStack
                        };

                        remainingCount -= canStack;

                        if (remainingCount <= 0)
                        {
                            // 모두 스택 완료
                            OnItemAdded?.Invoke(itemID, count);
                            SendItemToastMessage(item, count);
                            OnInventoryChanged?.Invoke();
                            return true;
                        }
                    }
                }
            }
        }

        // 2. 새 슬롯 생성 필요
        while (remainingCount > 0)
        {
            // 슬롯 개수 체크
            if (_slots.Count >= SlotCount)
            {
                SendWarningMessage("인벤토리가 가득 찼습니다!", Color.red);

                // 일부는 추가됐을 수 있음
                int addedCount = count - remainingCount;
                if (addedCount > 0)
                {
                    OnItemAdded?.Invoke(itemID, addedCount);
                    SendItemToastMessage(item, addedCount);
                    OnInventoryChanged?.Invoke();
                }

                return false;
            }

            // 새 슬롯에 추가
            int toAdd = Mathf.Min(remainingCount, maxCount);
            _slots.Add(new ItemSlotData
            {
                itemID = itemID,
                count = toAdd
            });

            remainingCount -= toAdd;
        }

        OnItemAdded?.Invoke(itemID, count);
        SendItemToastMessage(item, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public bool RemoveItem(int itemID, int count)
    {
        int totalCount = GetItemCount(itemID);

        if (totalCount < count)
        {
            Debug.LogWarning($"[InventoryData] 아이템 부족: {itemID} (필요: {count}, 보유: {totalCount})");
            return false;
        }

        int remainingToRemove = count;

        // 뒤에서부터 제거 (최신 획득 아이템부터)
        for (int i = _slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            if (_slots[i].itemID == itemID)
            {
                int slotCount = _slots[i].count;

                if (slotCount <= remainingToRemove)
                {
                    // 슬롯 전체 제거
                    remainingToRemove -= slotCount;
                    _slots.RemoveAt(i);
                }
                else
                {
                    // 슬롯 일부만 제거
                    _slots[i] = new ItemSlotData
                    {
                        itemID = itemID,
                        count = slotCount - remainingToRemove
                    };
                    remainingToRemove = 0;
                }
            }
        }

        OnItemRemoved?.Invoke(itemID, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 개수 조회 (모든 슬롯 합계)
    /// </summary>
    public int GetItemCount(int itemID)
    {
        return _slots.Where(slot => slot.itemID == itemID).Sum(slot => slot.count);
    }

    public void Clear()
    {
        _slots.Clear();
        OnInventoryChanged?.Invoke();
    }

    public bool TransferTo(IItemContainer target, int itemID, int count)
    {
        if (target == null || GetItemCount(itemID) < count)
            return false;

        if (!target.AddItem(itemID, count))
            return false;

        RemoveItem(itemID, count);
        return true;
    }

    public bool TransferAllTo(IItemContainer target)
    {
        if (target == null || !HasItems)
            return false;

        var slotsCopy = _slots.ToList();
        foreach (var slot in slotsCopy)
        {
            target.AddItem(slot.itemID, slot.count);
        }

        Clear();
        return true;
    }

    // ---- Tier 확장 ----
    public bool Expand(int targetTier)
    {
        if (targetTier < 1 || targetTier > 3)
        {
            //{ 4003002, 15 },
            { 4102001, 5 },
            { 4102031, 3 },
            { 4102007, 6 },
            { 4102009, 10 },
            //{ 4102003, 10},
            //{ 4102004, 10},
            //{ 4102006, 10},
            //{ 4102008, 10},
            //{ 4301002, 1},
            //{ 4302002, 1}
        };
        OnInventoryChanged?.Invoke();
        Debug.Log("[InventoryData] 인벤토리 초기화 완료");
    }
    public bool HasItem(int itemId, int amount) => GetItemCount(itemId) >= amount;
    public void Cleanup()
    {
        _items.Clear();
        OnItemAdded = null;
        OnItemRemoved = null;
        OnInventoryChanged = null;
        OnMessage = null;
    }
    public Dictionary<int, int> GetAllItems() => _items;

        int nextTier = inventoryTier + 1;

        if (targetTier != nextTier)
        {
            if (targetTier <= inventoryTier)
            {
                Debug.LogWarning($"[InventoryData] 이미 Tier {inventoryTier}입니다.");
            }
            else
            {
                Debug.LogWarning($"[InventoryData] Tier {targetTier}는 현재 적용 불가! (다음: Tier {nextTier})");
            }
            return false;
        }

        int oldTier = inventoryTier;
        int oldSlots = SlotCount;

        InventoryTier = targetTier;

        int newSlots = SlotCount;
        int addedSlots = newSlots - oldSlots;

        Debug.Log($"[인벤토리 확장] Tier {oldTier} → {targetTier} ({oldSlots}칸 → {newSlots}칸, +{addedSlots}칸)");

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool ExpandToNextTier()
    {
        int nextTier = inventoryTier + 1;

        if (nextTier > 3)
        {
            Debug.LogWarning("[InventoryData] 이미 최대 Tier입니다!");
            return false;
        }

        return Expand(nextTier);
    }

    // ---- 초기화 ----
    public void Initialize()
    {
        _slots.Clear();

        // 테스트 아이템
        AddItem(4102001, 5);
        AddItem(4102031, 3);

        inventoryTier = 0;
        OnInventoryChanged?.Invoke();

        Debug.Log($"[InventoryData] 인벤토리 초기화 완료 - Tier {inventoryTier}, {SlotCount}칸, {_slots.Count}개 아이템");
    }

    public bool HasItem(int itemId, int amount) => GetItemCount(itemId) >= amount;

    public void Cleanup()
    {
        _slots.Clear();
        OnItemAdded = null;
        OnItemRemoved = null;
        OnInventoryChanged = null;
        OnMessage = null;
    }

    /// <summary>
    /// 디버그/호환성용: Dictionary 형태로 반환
    /// </summary>
    public Dictionary<int, int> GetAllItems()
    {
        var dict = new Dictionary<int, int>();

        foreach (var slot in _slots)
        {
            if (dict.ContainsKey(slot.itemID))
            {
                dict[slot.itemID] += slot.count;
            }
            else
            {
                dict[slot.itemID] = slot.count;
            }
        }

        return dict;
    }

    private void SendItemToastMessage(ItemBase item, int amount)
        => OnItemToastMessage?.Invoke(item, amount);

    private void SendWarningMessage(string message, Color color)
        => OnWarningMessage?.Invoke(message, color);
}