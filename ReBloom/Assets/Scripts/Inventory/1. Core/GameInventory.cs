using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���� �κ��丮 ��Ʈ�ѷ�
/// ����Ͻ� ������ ������ ������ ���
/// </summary>
public class GameInventory : MonoBehaviour, IInventoryProvider
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameInventoryUI inventoryUI;
    [SerializeField] private QuickSlot quickSlot;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;


    #region IInventoryProvider ����
    public int GetItemCount(int itemId)
    {
        return inventoryData.GetItemCount(itemId);
    }

    public void AddItem(int itemId, int amount)
    {
        inventoryData.AddItem(itemId, amount);
    }

    public void RemoveItem(int itemId, int amount)
    {
        inventoryData.RemoveItem(itemId, amount);
    }

    public void Clear()
    {
        inventoryData.Clear();
    }

    public bool HasItem(int itemId, int amount)
    {
        return inventoryData.HasItem(itemId, amount);
    }

    public void Consume(int itemId, int amount)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);

        if (item != null)
        {
            if (item.canUseable)
            {
                RemoveItem(itemId, amount);
                item.Apply(playerController);

                inventoryData.SendMessage($"{item.itemName}��(��) {amount}�� ����߽��ϴ�.");
            }
            else if (item.canEquip)
            {
                RemoveItem(itemId, amount);
                item.Apply(playerController);

                inventoryData.SendMessage($"{item.itemName}��(��) {amount}�� �����߽��ϴ�.");
            }
            else
            {
                inventoryData.SendMessage($"{item?.itemName}��(��) ����� �� �����ϴ�.");
            }
        }
        else
        {
            Debug.LogError("[GameInventory] ������ �����Ͱ� �����ϴ�.");
        }
    }
    #endregion

    #region ���͸� & ����
    /// <summary>
    /// ���̺� Ÿ�Ժ��� ������ ���͸�
    /// </summary>
    public Dictionary<int, int> GetItemsByTable(ItemTableType tableType)
    {
        var filtered = new Dictionary<int, int>();

        foreach (var itemPair in inventoryData.Items)
        {
            int itemId = itemPair.Key;
            ItemTableType itemTableType = ItemIDParser.GetTableType(itemId);

            if (itemTableType == tableType)
            {
                filtered.Add(itemId, itemPair.Value);
            }
        }

        return filtered;
    }
    private void OnDestroy()
    {
        if (quickSlot != null)
        {
            //quickSlot.OnSlotAssign -= AssignQuickSlot;
        }
    }

    /// <summary>
    /// ��� ������ ��������
    /// </summary>
    public Dictionary<int, int> GetAllItems()
    {
        return new Dictionary<int, int>(inventoryData.Items);
    }

    /// <summary>
    /// ������ ���� (ID ����)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItems(ItemTableType? tableType = null)
    {
        var items = tableType.HasValue
            ? GetItemsByTable(tableType.Value)
            : GetAllItems();

        return items.OrderBy(x => x.Key).ToList();
    }

    /// <summary>
    /// ������ ���� (�̸� ����)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItemsByName(ItemTableType? tableType = null)
    {
        var items = tableType.HasValue
            ? GetItemsByTable(tableType.Value)
            : GetAllItems();

        return items.OrderBy(x => ItemDatabase.I.GetItem(x.Key)?.itemName ?? "").ToList();
    }

    /// <summary>
    /// ������ ���� (���� ����)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItemsByQuantity(ItemTableType? tableType = null, bool descending = true)
    {
        var items = tableType.HasValue
            ? GetItemsByTable(tableType.Value)
            : GetAllItems();

        return descending
            ? items.OrderByDescending(x => x.Value).ToList()
            : items.OrderBy(x => x.Value).ToList();
    }

    /// <summary>
    /// ������ ��ġ ������ �����۸� ���͸�
    /// </summary>
    public List<KeyValuePair<int, int>> GetQuickSlotableItems()
    {
        var result = new List<KeyValuePair<int, int>>();

        foreach (var itemPair in inventoryData.Items)
        {
            ItemBase item = ItemDatabase.I.GetItem(itemPair.Key);
            if (item != null && item.canQuickSlot)
            {
                result.Add(itemPair);
            }
        }

        return result;
    }
    #endregion

    //#region ������ ����
    //private bool CanAssignQuickSlot(int itemId)
    //{
    //    ItemBase item = ItemDatabase.I.GetItem(itemId);
    //    return item != null && item.canQuickSlot;
    //}
    //#endregion

    #region UI ����
    public void OpenInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.ToggleInventory();
        }
    }

    public void CloseInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.ToggleInventory();
        }
    }
    #endregion
}