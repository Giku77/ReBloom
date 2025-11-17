using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "ReBloom/Inventory/GameInventory Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] private int maxInventorySlots = 10;

    private Dictionary<int, int> _items = new Dictionary<int, int>();

    // �̺�Ʈ
    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;
    public event Action<string> OnMessage;

    public Dictionary<int, int> Items => _items;
    public int MaxSlots => maxInventorySlots;

    /// <summary>
    /// �޽��� ���� (�ܺο��� ȣ�� ����)
    /// </summary>
    public void SendMessage(string message)
    {
        OnMessage?.Invoke(message);
        Debug.Log($"[InventoryData] {message}");
    }

    public void Initialize()
    {
        _items = new Dictionary<int, int>()
        {
            { 4003002, 15 },
            { 4102001, 12 },
            { 4102002, 6 },
            { 4102005, 10 },
            { 4102003, 10},
            { 4102004, 10},
            { 4102006, 10},
            { 4102008, 10},
            { 4301002, 1},
            { 4302002, 1}
        };

        OnInventoryChanged?.Invoke();
        Debug.Log("[InventoryData] �ʱ�ȭ �Ϸ�");
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
            SendMessage($"{ItemDatabase.I.GetItem(itemId).itemName}��(��) {amount}�� ȹ���߽��ϴ�.");
        }
        else
        {
            if (_items.Count >= maxInventorySlots)
            {
                SendMessage($"�ִ� ����({maxInventorySlots}��)�� �����Ͽ� ȹ�� ����!");
                Debug.LogWarning($"[�κ��丮] ������ ���� ��!");
                return;
            }

            _items[itemId] = amount;
            OnItemAdded?.Invoke(itemId, amount);
            SendMessage($"{ItemDatabase.I.GetItem(itemId).itemName}��(��) {amount}�� ȹ���߽��ϴ�.");
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

    public Dictionary<int, int> GetAllItems()
    {
        if (_items != null)
        {
            return _items;
        }
        return null;
    }
}