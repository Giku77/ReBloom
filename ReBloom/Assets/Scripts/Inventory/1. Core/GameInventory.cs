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


    [Header("Manager")]
    [SerializeField] private ItemSpawner itemSpawner;

    public IItemContainer Container => inventoryData;

    //private int currentEquippedToolId = -1;        // 도구
    //private int currentEquippedClothingId = -1;    // 옷
    //private int currentEquippedShoesId = -1;       // 신발
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
        quickSlot?.SyncInventoryQuickSlots();
    }

    private void OnEnable()
    {
        //quickSlot?.SyncInventoryQuickSlots();
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
        if (item == null) return;

        if (item.canUseable)
        {
            bool success = item.Apply(playerController);
            if (success) RemoveItem(itemId, amount);

            if (item.itemID == 4002001 || item.itemID == 4002002)
            {
                AddItem(4102035, 1); //@??
            }
        }
        else if (item.canEquip)
        {
            // PlayerEquipManager에게 위임
            playerEquipmanager.ToggleEquip(itemId);  // 토글 처리도 위임
        }
    }
    #endregion

    /// <summary>
    /// add 아이템 시도 후 overflow 만큼 월드에 드롭
    /// 실제 추가된 수량 반환
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="amount"></param>
    public int AddItemFromWorld(int itemId, int amount)
    {
        int added = inventoryData.AddItemWithOverflow(itemId, amount, out int overflow);

        if (overflow > 0)
        {
            DropOverflow(itemId, overflow);
        }

        return added;
    }

    /// <summary>
    /// 드롭 없이 가득 찰 때까지만 Add 하고 끝내는 경우
    /// 하나라도 overflow 발생 시 false
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="amount"></param>
    public bool TryAddItemFromWorld(int itemId, int amount)
    {
        inventoryData.AddItemWithOverflow(itemId, amount, out int overflow);
        return overflow == 0;
    }

    private void DropOverflow(int itemId, int amount)
    {
        var item = ItemDatabase.I.GetItem(itemId);
        if (item == null) return;

        Vector3 dropPos = playerController.transform.position + Vector3.up * 0.5f;
        itemSpawner.DropItemWithQuantity(item, dropPos, amount).Forget();
    }

    /// <summary>
    /// 도구 장착/해제 토글 (외부 호출용)
    /// </summary>
    public bool ToggleEquip(int itemId)
    {
        //equipmanager가 처리
        return playerEquipmanager.ToggleEquip(itemId);
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