using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "Game/Inventory Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] private int maxInventorySlots = 10;

    private Dictionary<int, int> _items = new Dictionary<int, int>();

    // 이벤트
    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;
    public event Action<string> OnMessage;

    public Dictionary<int, int> Items => _items;
    public int MaxSlots => maxInventorySlots;

    /// <summary>
    /// 메시지 전송 (외부에서 호출 가능)
    /// </summary>
    public void SendMessage(string message)
    {
        OnMessage?.Invoke(message);
        Debug.Log($"[InventoryData] {message}");
    }

    public void Initialize()
    {
        _items = new Dictionary<int, int>();
        //{
        //    { 4003002, 15 },
        //    { 4102002, 6 },
        //    { 4102005, 10 },
        //};

        OnInventoryChanged?.Invoke();
        Debug.Log("[InventoryData] 초기화 완료");
    }

    public int GetItemCount(int itemId)
    {
        return _items.TryGetValue(itemId, out var cnt) ? cnt : 0;
    }

    public void AddItem(int itemId, int amount)
    {
        if (_items.ContainsKey(itemId))
        {
            _items[itemId] += amount;
            OnItemAdded?.Invoke(itemId, amount);
            SendMessage($"{ItemDatabase.I.GetItem(itemId).itemName}을(를) {amount}개 획득했습니다.");
        }
        else
        {
            if (_items.Count >= maxInventorySlots)
            {
                SendMessage($"최대 개수({maxInventorySlots}개)에 도달하여 획득 실패!");
                Debug.LogWarning($"[인벤토리] 슬롯이 가득 참!");
                return;
            }

            _items[itemId] = amount;
            OnItemAdded?.Invoke(itemId, amount);
            SendMessage($"{ItemDatabase.I.GetItem(itemId).itemName}을(를) {amount}개 획득했습니다.");
        }

        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(int itemId, int amount)
    {
        if (_items.ContainsKey(itemId))
        {
            _items[itemId] -= amount;

            if (_items[itemId] <= 0)
            {
                _items.Remove(itemId);
            }

            OnItemRemoved?.Invoke(itemId, amount);
            OnInventoryChanged?.Invoke();
        }
    }

    public void Clear()
    {
        _items.Clear();
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public void Cleanup()
    {
        _items.Clear();
        OnItemAdded = null;
        OnItemRemoved = null;
        OnInventoryChanged = null;
        OnMessage = null;
    }
}