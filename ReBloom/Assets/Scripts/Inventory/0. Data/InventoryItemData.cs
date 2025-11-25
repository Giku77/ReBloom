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

    // IItemContainer 인터페이스 구현: SlotCount
    // getter는 Tier 기반 계산, setter는 무시 (Tier로만 변경)
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
            // IItemContainer 요구사항이지만, Tier 시스템에선 직접 설정 안함
            // 필요시 역계산으로 Tier 설정
            if (value <= 10) InventoryTier = 0;
            else if (value <= 20) InventoryTier = 1;
            else if (value <= 30) InventoryTier = 2;
            else InventoryTier = 3;
        }
    }

    private Dictionary<int, int> _items = new Dictionary<int, int>();

    // ---- 인벤토리 전용 이벤트---
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

    // IItemContainer용 래퍼 (Dictionary -> List 변환)
    public IReadOnlyList<ItemSlotData> Items =>
        _items.Select(kvp => new ItemSlotData { itemID = kvp.Key, count = kvp.Value }).ToList();

    public bool HasItems => _items.Count > 0;


    // ---- IItemContainer 메서드 구현 ----
    public bool AddItem(int itemID, int count)
    {
        var item = ItemDatabase.I.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[InventoryData] 아이템 ID {itemID}를 찾을 수 없습니다!");
            return false;
        }

        if (_items.ContainsKey(itemID))
        {
            _items[itemID] += count;
        }
        else
        {
            if (_items.Count >= SlotCount)
            {
                SendWarningMessage("인벤토리가 가득 찼습니다!", Color.red);
                return false;
            }
            _items[itemID] = count;
        }

        OnItemAdded?.Invoke(itemID, count);
        SendItemToastMessage(item, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 인벤토리 Tier 확장 (확장칩용)
    /// </summary>
    public bool Expand(int targetTier)
    {
        if (targetTier < 1 || targetTier > 3)
        {
            Debug.LogError($"[InventoryData] 잘못된 Tier: {targetTier} (1~3만 가능)");
            return false;
        }

        if (inventoryTier >= targetTier)
        {
            Debug.LogWarning($"[InventoryData] 이미 Tier {inventoryTier}입니다. 목표: Tier {targetTier}");
            return false;
        }

        int oldTier = inventoryTier;
        int oldSlots = SlotCount;

        InventoryTier = targetTier;

        int newSlots = SlotCount;
        int addedSlots = newSlots - oldSlots;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 다음 Tier로 업그레이드 (간편 메서드)
    /// </summary>
    public bool ExpandToNextTier()
    {
        int nextTier = inventoryTier + 1;
        return Expand(nextTier);
    }

    public bool RemoveItem(int itemID, int count)
    {
        if (!_items.ContainsKey(itemID) || _items[itemID] < count)
            return false;

        _items[itemID] -= count;
        if (_items[itemID] <= 0)
            _items.Remove(itemID);

        OnItemRemoved?.Invoke(itemID, count);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(int itemID)
    {
        return _items.TryGetValue(itemID, out var cnt) ? cnt : 0;
    }

    public void Clear()
    {
        _items.Clear();
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

        var itemsCopy = _items.ToList();
        foreach (var kvp in itemsCopy)
        {
            target.AddItem(kvp.Key, kvp.Value);
        }

        Clear();
        return true;
    }

    // ---- 인벤토리 전용 메서드 ----
    public void Initialize()
    {
        _items = new Dictionary<int, int>()
        {
            { 4102001, 5 },
            { 4102031, 3 },
        };
        inventoryTier = 0;
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

    private void SendItemToastMessage(ItemBase item, int amount)
        => OnItemToastMessage?.Invoke(item, amount);

    private void SendWarningMessage(string message, Color color)
        => OnWarningMessage?.Invoke(message, color);
}

//using System;
//using System.Collections.Generic;
//using UnityEngine;

//[CreateAssetMenu(fileName = "InventoryItemData", menuName = "ReBloom/Inventory/GameInventory Data")]
//public class InventoryItemData : ScriptableObject, IItemContainer
//{
//    [Header("Settings")]
//    [SerializeField] private int maxSlot = 10; //TODO: 확장 아이템 구현시 변경

//    private Dictionary<int, int> _items = new Dictionary<int, int>();

//    // 이벤트
//    public event Action<int, int> OnItemAdded;
//    public event Action<int, int> OnItemRemoved;
//    public event Action OnInventoryChanged;
//    public event Action<string> OnMessage;
//    public event Action<ItemBase, int> OnItemToastMessage;
//    public event Action<string, Color> OnWarningMessage;

//    public Dictionary<int, int> Items => _items;
//    public int MaxSlots => maxSlot;

//    /// <summary>
//    /// 메시지 전송 (인벤토리 관련 메시지 출력)
//    /// </summary>
//    public void SendMessage(string message)
//    {
//        OnMessage?.Invoke(message);
//        Debug.Log($"[InventoryData] {message}");
//    }

//    /// <summary>
//    /// 아이템 토스트 메시지 전송 (ItemBase 기반)
//    /// </summary>
//    private void SendItemToastMessage(ItemBase item, int amount)
//    {
//        OnItemToastMessage?.Invoke(item, amount);
//    }

//    /// <summary>
//    /// 경고 메시지 전송
//    /// </summary>
//    private void SendWarningMessage(string message, Color color = default)
//    {
//        if (color == default) color = Color.red;
//        OnWarningMessage?.Invoke(message, color);
//    }
//    public void Initialize()
//    {
//        _items = new Dictionary<int, int>()
//        {
//            //{ 4003002, 15 },
//            //{ 4102001, 12 },
//            //{ 4102002, 6 },
//            //{ 4102005, 10 },
//            //{ 4102003, 10},
//            //{ 4102004, 10},
//            //{ 4102006, 10},
//            //{ 4102008, 10},
//            //{ 4301002, 1},
//            //{ 4302002, 1}
//        };

//        OnInventoryChanged?.Invoke();
//        Debug.Log("[InventoryData] 인벤토리 초기화 완료");
//    }

//    public int GetItemCount(int itemId)
//    {
//        return _items.TryGetValue(itemId, out var cnt) ? cnt : 0;
//    }

//    public void AddItem(int itemId, int amount)
//    {
//        var item = ItemDatabase.I.GetItem(itemId);

//        if (item == null)
//        {
//            Debug.LogError($"[InventoryData] 아이템 ID {itemId}를 찾을 수 없습니다!");
//            return;
//        }

//        if (_items.ContainsKey(itemId))
//        {
//            _items[itemId] += amount;
//            OnItemAdded?.Invoke(itemId, amount);

//            // ItemBase와 수량 직접 전달
//            SendItemToastMessage(item, amount);
//        }
//        else
//        {
//            if (_items.Count >= maxSlot)
//            {
//                SendMessage($"인벤토리 슬롯({maxSlot}개)이 모두 찼습니다!");

//                // 경고 메시지 전송
//                SendWarningMessage("인벤토리가 가득 찼습니다!", Color.red);

//                Debug.LogWarning($"[인벤토리] 슬롯이 모두 찼습니다!");
//                return;
//            }

//            _items[itemId] = amount;
//            OnItemAdded?.Invoke(itemId, amount);

//            // ItemBase와 수량 직접 전달
//            SendItemToastMessage(item, amount);
//        }

//        OnInventoryChanged?.Invoke();
//    }

//    public void RemoveItem(int itemId, int amount)
//    {
//        if (_items.ContainsKey(itemId))
//        {
//            _items[itemId] -= amount;

//            if (_items[itemId] <= 0)
//            {
//                _items.Remove(itemId);
//            }

//            OnItemRemoved?.Invoke(itemId, amount);
//            OnInventoryChanged?.Invoke();
//        }
//    }

//    public void Clear()
//    {
//        _items.Clear();
//        OnInventoryChanged?.Invoke();
//    }

//    public bool HasItem(int itemId, int amount)
//    {
//        return GetItemCount(itemId) >= amount;
//    }

//    public void Cleanup()
//    {
//        _items.Clear();
//        OnItemAdded = null;
//        OnItemRemoved = null;
//        OnInventoryChanged = null;
//        OnMessage = null;
//    }

//    public Dictionary<int, int> GetAllItems()
//    {
//        if (_items != null)
//        {
//            return _items;
//        }
//        return null;
//    }
//}