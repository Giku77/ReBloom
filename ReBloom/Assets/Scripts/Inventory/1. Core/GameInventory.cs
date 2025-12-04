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
    [SerializeField] private PlayerEquipManager playerEquipmanager;

    public IItemContainer Container => inventoryData;
    private int currentEquippedToolId = -1;        // 도구
    private int currentEquippedClothingId = -1;    // 옷
    private int currentEquippedShoesId = -1;       // 신발
    private void Awake()
    {
        var player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerEquipmanager = player.GetComponent<PlayerEquipManager>();
        //// 초기화
    }
    private void Start()
    {
        inventoryData.Initialize(); 
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
    public bool TransferTo(IItemContainer container, int itemId, int amount) => inventoryData.TransferTo(container, itemId, amount);
    public bool SwapSlots(int fromIndex, int toIndex) => inventoryData.SwapSlots(fromIndex, toIndex);
    public void Consume(int itemId, int amount)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        if (item == null)
        {
            Debug.LogError("[GameInventory] 아이템을 찾을 수 없습니다.");
            return;
        }

        if (item.canUseable)
        {
            // 소비 아이템: 사용 후 제거
            bool success = item.Apply(playerController);
            if (success) RemoveItem(itemId, amount);
            return;
        }
        else if (item.canEquip)
        {
            // 타입별로 분기 처리
            if (item is ToolItemData tool)
            {
                //HandleToolEquip(tool, itemId);
                playerEquipmanager.EquipItem(itemId);
            }
            else if (item is ProtectiveItemData protective)
            {
                //HandleProtectiveEquip(protective, itemId);
                playerEquipmanager.EquipItem(itemId);
            }
            return;
        }
    }
    #endregion

    // 도구 장착 처리
    private void HandleToolEquip(ToolItemData item, int itemId)
    {
        // 같은 도구 클릭 = 토글
        if (currentEquippedToolId == itemId)
        {
            item.UnApply(playerController);
            AddItem(item.itemID, 1); // 임의로 추가
            currentEquippedToolId = -1;
            Debug.Log($"[GameInventory] {item.itemName} 장착 해제");
            return;
        }

        // 다른 도구로 교체
        if (currentEquippedToolId != -1)
        {
            ItemBase previousTool = ItemDatabase.I.GetItem(currentEquippedToolId);
            previousTool?.UnApply(playerController);
        }

        // 새 도구 장착
        bool success = item.Apply(playerController);
        if (success)
        {
            currentEquippedToolId = itemId;
            Debug.Log($"[GameInventory] {item.itemName} 장착");
        }
    }

    // 보호구 장착 처리
    private void HandleProtectiveEquip(ProtectiveItemData item, int itemId)
    {
        switch (item.gearType)
        {
            case GearType.Clothing:
                // 같은 옷 클릭 = 토글 // 12.04 수정사항: 인벤토리에서 빠지면서 같은 옷 클릭 안됨
                if (currentEquippedClothingId == itemId)
                {
                    item.UnApply(playerController);
                    AddItem(item.itemID, 1); // 임의로 추가
                    currentEquippedClothingId = -1;
                    Debug.Log($"[GameInventory] {item.itemName} 장착 해제");
                    return;
                }

                // 다른 옷으로 교체
                if (currentEquippedClothingId != -1)
                {
                    ItemBase previousCloth = ItemDatabase.I.GetItem(currentEquippedClothingId);
                    previousCloth?.UnApply(playerController);
                }

                // 새 옷 장착, 장착한 아이템은 이 함수 바깥에서 인벤토리 슬롯 제거
                bool clothSuccess = item.Apply(playerController);
                if (clothSuccess)
                {
                    currentEquippedClothingId = itemId;
                    Debug.Log($"[GameInventory] {item.itemName} 장착");
                }
                break;

            case GearType.Shoes:
                // 같은 신발 클릭 = 토글
                if (currentEquippedShoesId == itemId)
                {
                    item.UnApply(playerController);
                    AddItem(item.itemID, 1); // 임의로 추가
                    currentEquippedShoesId = -1;
                    Debug.Log($"[GameInventory] {item.itemName} 장착 해제");
                    return;
                }

                // 다른 신발로 교체
                if (currentEquippedShoesId != -1)
                {
                    ItemBase previousShoes = ItemDatabase.I.GetItem(currentEquippedShoesId);
                    previousShoes?.UnApply(playerController);
                }

                // 새 신발 장착
                bool shoesSuccess = item.Apply(playerController);
                if (shoesSuccess)
                {
                    currentEquippedShoesId = itemId;
                    Debug.Log($"[GameInventory] {item.itemName} 장착");
                }
                break;

            default:
                Debug.LogWarning($"[GameInventory] 알 수 없는 보호구 타입: {item.gearType}");
                break;
        }
    }

    /// <summary>
    /// 도구 장착/해제 토글 (외부 호출용)
    /// </summary>
    public bool ToggleEquip(int itemId)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        if (item == null || !item.canEquip)
            return false;

        // 타입별로 분기
        if (item is ToolItemData tool)
        {
            HandleToolEquip(tool, itemId);
            return true;
        }
        else if (item is ProtectiveItemData protective)
        {
            HandleProtectiveEquip(protective, itemId);
            return true;
        }

        return false;
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
    public void OpenInventory() {
        inventoryUI.ToggleInventory();
        TutorialEventBus.RaiseAction((int)TutorialActionId.OpenInventory);
    }
    public void CloseInventory() => inventoryUI.ToggleInventory();
    #endregion
}