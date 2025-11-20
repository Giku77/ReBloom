using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStorageData", menuName = "ReBloom/Build/StorageData")]
public class StorageData : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] private int maxStorageSlots; //TODO: 창고 수량 기획에 맞게 조정

    private Dictionary<int, int> storagedItems = new Dictionary<int, int>();

    // 이벤트
    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnStorageChanged;
    public event Action<ItemBase, int> OnItemToastMessage;
    public event Action<string> OnWarningMessage;

    public int MaxSlots => maxStorageSlots;

    /// <summary>
    /// 아이템 토스트 메시지 전송 (ItemBase 기반)
    /// </summary>
    private void SendItemToastMessage(ItemBase item, int amount)
    {
        OnItemToastMessage?.Invoke(item, amount);
    }
    public int GetItemCount(int itemId)
    {
        return storagedItems.TryGetValue(itemId, out var cnt) ? cnt : 0;
    }
    /// <summary>
    /// 경고 메시지 전송
    /// </summary>
    private void SendWarningMessage(string message)
    {
        OnWarningMessage?.Invoke(message);
    }

    public void Initialize()
    {
        storagedItems = new Dictionary<int, int>()
        {

        };
        Debug.Log("[StorageData] 창고 초기화 완료");
    }
    public void RemoveItem(int itemId, int amount)
    {
        if (storagedItems.ContainsKey(itemId))
        {
            storagedItems[itemId] -= amount;

            if (storagedItems[itemId] <= 0)
            {
                storagedItems.Remove(itemId);
            }

            OnItemRemoved?.Invoke(itemId, amount);
        }
    }

    public void Clear()
    {
        storagedItems.Clear();
    }

    public bool HasItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public void Cleanup()
    {
        storagedItems.Clear();
        OnItemAdded = null;
        OnItemRemoved = null;
    }

    public Dictionary<int, int> GetAllItems()
    {
        if (storagedItems != null)
        {
            return storagedItems;
        }
        return null;
    }
}