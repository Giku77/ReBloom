using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 인벤토리 컨트롤러
/// 비즈니스 로직과 데이터 조작을 담당
/// </summary>
public class GameInventory : MonoBehaviour, IInventoryProvider
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameInventoryUI inventoryUI;
    [SerializeField] private QuickSlot quickSlot;

    #region IInventoryProvider 구현
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

        if (item != null && item.canUseable)
        {
            RemoveItem(itemId, amount);
            inventoryData.SendMessage($"{item.itemName}을(를) {amount}개 사용했습니다.");
        }
        else
        {
            inventoryData.SendMessage($"{item?.itemName ?? "알 수 없는 아이템"}은(는) 사용할 수 없습니다.");
        }
    }
    #endregion

    #region 필터링 & 정렬
    /// <summary>
    /// 테이블 타입별로 아이템 필터링
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

    /// <summary>
    /// 모든 아이템 가져오기
    /// </summary>
    public Dictionary<int, int> GetAllItems()
    {
        return new Dictionary<int, int>(inventoryData.Items);
    }

    /// <summary>
    /// 아이템 정렬 (ID 기준)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItems(ItemTableType? tableType = null)
    {
        var items = tableType.HasValue
            ? GetItemsByTable(tableType.Value)
            : GetAllItems();

        return items.OrderBy(x => x.Key).ToList();
    }

    /// <summary>
    /// 아이템 정렬 (이름 기준)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItemsByName(ItemTableType? tableType = null)
    {
        var items = tableType.HasValue
            ? GetItemsByTable(tableType.Value)
            : GetAllItems();

        return items.OrderBy(x => ItemDatabase.I.GetItem(x.Key)?.itemName ?? "").ToList();
    }

    /// <summary>
    /// 아이템 정렬 (수량 기준)
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
    /// 퀵슬롯 배치 가능한 아이템만 필터링
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

    //#region 퀵슬롯 관련
    //private bool CanAssignQuickSlot(int itemId)
    //{
    //    ItemBase item = ItemDatabase.I.GetItem(itemId);
    //    return item != null && item.canQuickSlot;
    //}
    //#endregion

    #region UI 제어
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