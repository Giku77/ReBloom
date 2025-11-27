using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameInventory : MonoBehaviour, IInventoryProvider
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameInventoryUI inventoryUI;
    [SerializeField] private QuickSlot quickSlot;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    private int currentEquippedToolId = -1;
    private void Awake()
    {
        playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        quickSlot?.SyncInventoryQuickSlots();
    }

    #region IInventoryProvider 구현
    public int GetItemCount(int itemId) => inventoryData.GetItemCount(itemId);
    public void AddItem(int itemId, int amount) => inventoryData.AddItem(itemId, amount);
    public void RemoveItem(int itemId, int amount) => inventoryData.RemoveItem(itemId, amount);
    public void Clear() => inventoryData.Clear();
    public bool HasItem(int itemId, int amount) => inventoryData.HasItem(itemId, amount);

    public bool Consume(int itemId, int amount)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        if (item == null)
        {
            Debug.LogError("[GameInventory] 아이템을 찾을 수 없습니다.");
            return false;
        }

        if (item.canUseable)
        {
            // 소비 아이템: 사용 후 제거
            bool success = item.Apply(playerController);
            if (success) RemoveItem(itemId, amount);
            return success;
        }
        else if (item.canEquip)
        {
            // 이미 장착된 도구인지 확인
            if (currentEquippedToolId == itemId)
            {
                Debug.Log($"[GameInventory] {item.itemName}은(는) 이미 장착 중");
                return false;
            }

            // 이전 도구 장착 해제
            if (currentEquippedToolId != -1)
            {
                ItemBase previousTool = ItemDatabase.I.GetItem(currentEquippedToolId);
                previousTool?.UnApply(playerController); // UnApply 메서드 필요
            }

            // 새 도구 장착
            bool success = item.Apply(playerController);
            if (success)
            {
                currentEquippedToolId = itemId;
                // 장비는 인벤토리에서 제거하지 않음
            }
            return success;
        }
        return false;
    }
    #endregion

    /// <summary>
    /// 도구 장착/해제 토글
    /// </summary>
    public bool ToggleEquip(int itemId)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        if (item == null || !item.canEquip)
            return false;

        // 현재 장착 중인 아이템인지 확인
        if (currentEquippedToolId == itemId)
        {
            // 같은 아이템 = 장착 해제
            item.UnApply(playerController);
            currentEquippedToolId = -1;
            Debug.Log($"[GameInventory] {item.itemName} 장착 해제");
            return true;
        }
        else
        {
            // 다른 아이템 = 이전 아이템 해제 후 새 아이템 장착
            if (currentEquippedToolId != -1)
            {
                ItemBase previousItem = ItemDatabase.I.GetItem(currentEquippedToolId);
                previousItem?.UnApply(playerController);
            }

            bool success = item.Apply(playerController);
            if (success)
            {
                currentEquippedToolId = itemId;
                Debug.Log($"[GameInventory] {item.itemName} 장착");
            }
            return success;
        }
    }

    #region 아이템 & 카테고리 분류

        /// <summary>
        /// 인벤토리 카테고리 별 아이템 필터링
        /// </summary>
    public Dictionary<int, int> GetItemsByInventoryType(InventorySlotType inventoryType)
    {
        var filtered = new Dictionary<int, int>();

        // ItemSlotData로 변경
        foreach (var slot in inventoryData.Items)
        {
            InventorySlotType itemInventoryType = ItemIDParser.GetInventoryType(slot.itemID);

            if (itemInventoryType == inventoryType)
            {
                filtered.Add(slot.itemID, slot.count);
            }
        }

        return filtered;
    }

    /// <summary>
    /// 모든 아이템 가져오기
    /// </summary>
    public Dictionary<int, int> GetAllItems()
    {
        // itemSlotData -> Dictionary 변환
        return inventoryData.Items.ToDictionary(
            slot => slot.itemID,
            slot => slot.count
        );
    }
    /// <summary>
    /// 모든 슬롯 가져오기 (슬롯 기반)
    /// </summary>
    public IReadOnlyList<ItemSlotData> GetAllSlots()
    {
        return inventoryData.Items;
    }
    /// <summary>
    /// 아이템 정렬 (ID 기준)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItems(InventorySlotType? invtType = null)
    {
        if (invtType == null)
        {
            return GetAllItems().ToList();
        }

        var items = invtType.HasValue
            ? GetItemsByInventoryType(invtType.Value)
            : GetAllItems();

        return items.OrderBy(x => x.Key).ToList();
    }

    /// <summary>
    /// 아이템 정렬 (이름 기준)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItemsByName(InventorySlotType? invtType = null)
    {
        var items = invtType.HasValue
            ? GetItemsByInventoryType(invtType.Value)
            : GetAllItems();

        return items.OrderBy(x => ItemDatabase.I.GetItem(x.Key)?.itemName ?? "").ToList();
    }

    /// <summary>
    /// 아이템 정렬 (수량 기준)
    /// </summary>
    public List<KeyValuePair<int, int>> GetSortedItemsByQuantity(InventorySlotType? invtType = null, bool descending = true)
    {
        var items = invtType.HasValue
            ? GetItemsByInventoryType(invtType.Value)
            : GetAllItems();

        return descending
            ? items.OrderByDescending(x => x.Value).ToList()
            : items.OrderBy(x => x.Value).ToList();
    }

    /// <summary>
    /// 퀵슬롯에 할당 가능한 아이템만 반환
    /// </summary>
    public List<KeyValuePair<int, int>> GetQuickSlotableItems()
    {
        var result = new List<KeyValuePair<int, int>>();

        // ItemSlotData로 변경
        foreach (var slot in inventoryData.Items)
        {
            ItemBase item = ItemDatabase.I.GetItem(slot.itemID);
            if (item != null && item.canQuickSlot)
            {
                result.Add(new KeyValuePair<int, int>(slot.itemID, slot.count));
            }
        }

        return result;
    }
    #endregion

    #region UI 제어
    public void OpenInventory() => inventoryUI?.ToggleInventory();
    public void CloseInventory() => inventoryUI?.ToggleInventory();
    #endregion
}