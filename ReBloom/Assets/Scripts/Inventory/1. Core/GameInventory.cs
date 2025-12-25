using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameInventory : MonoBehaviour, IGameInventory
{
    //Core Systems
    private InventoryItemData containerData;     // 순수 데이터
    private ItemDropService dropService;         // 드롭 처리
    private InventoryMessageService uiService;        // UI 처리

    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameInventoryUI inventoryUI;
    [SerializeField] private QuickSlot quickSlot;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerEquipManager playerEquipmanager;
    [SerializeField] private InventoryRobotPet robotPet;

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
        InitializeServices();
        //// 초기화
    }
    private void Start()
    {
        inventoryData.Initialize();
        quickSlot?.SyncInventoryQuickSlots();
    }
    private void InitializeServices()
    {
        if (dropService == null)
            dropService = GetComponent<ItemDropService>();

        if (uiService == null)
            uiService = GetComponent<InventoryMessageService>();
    }
    private void OnEnable()
    {
        if (inventoryData != null)
            inventoryData.OnContainerChanged += OnInvChanged;
    }
    private void OnDisable()
    {
        if (inventoryData != null)
            inventoryData.OnContainerChanged -= OnInvChanged;
    }

    private void OnInvChanged()
    {
        AutoSaveService.I?.RequestSave("InventoryChanged");
    }

    #region 단순 위임 메서드들
    public int GetItemCount(int itemID)
        => inventoryData.GetItemCount(itemID);

    // HasItem 중복 제거 - 하나만 남김
    public bool HasItem() => inventoryData.HasItems || playerEquipmanager.ExistEquipItem;
    public bool HasItem(int itemID, int count)
        => inventoryData.HasItem(itemID, count);
   
    public void RemoveItem(int itemID, int count)
        => inventoryData.TryRemoveItem(itemID, count);
    //public int AddItem(int itemID, int count)
    //    => inventoryData.TryAddItem(itemID, count);
    public void Clear()
        => inventoryData.Clear();

    public bool TransferTo(IItemContainer target, int itemID, int count)
    {
        if (GetItemCount(itemID) < count)
            return false;

        int added = target.TryAddItem(itemID, count);  // 인터페이스 메서드명
        if (added > 0)
        {
            inventoryData.TryRemoveItem(itemID, added);  // inventoryData 사용
        }

        return added == count;
    }

    public bool SwapSlots(int fromIndex, int toIndex) => inventoryData.SwapSlots(fromIndex, toIndex);
    public void Consume(int itemId, int amount)
    {
        UseItem(itemId, amount);

        //ItemBase item = ItemDatabase.I.GetItem(itemId);
        //if (item == null) return;

        //if (item.canUseable)
        //{
        //    bool success = item.Apply(playerController);
        //    if (success) RemoveItem(itemId, amount);

        //    if (item.itemID == 4002001 || item.itemID == 4002002)
        //    {
        //        AddItemFromWorld(4102035, 1); //@??
        //    }
        //}
        //else if (item.canEquip)
        //{
        //    // PlayerEquipManager에게 위임
        //    playerEquipmanager.ToggleEquip(itemId);  // 토글 처리도 위임
        //}
    }
    #endregion

    /// <summary>
    /// add 아이템 시도 후 overflow 만큼 월드에 드롭
    /// 실제 추가된 수량 반환
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="count"></param>
    public int AddItemFromWorld(int itemID, int count, bool drop = false)
    {
        int added = inventoryData.TryAddItem(itemID, count);  // TryAddItem 사용
        int overflow = count - added;

        if (added > 0)
        {
            var item = ItemDatabase.I.GetItem(itemID);
            uiService?.ShowItemAcquired(item, added);
            InventroyEventSystem.ItemAcquiredTier(item.tier);
        }

        if (overflow > 0)
        {
            robotPet?.PlayPoppyVoice(80052);

            if (added > 0 || drop)
            {
                dropService?.DropItem(itemID, overflow);
                uiService?.ShowWarning($"인벤토리가 가득 차서 {overflow}개는 드랍되었습니다.");
            }
            else
            {
                uiService?.ShowWarning("인벤토리가 가득 찼습니다!");
            }

            InventroyEventSystem.InventoryFull();
        }

        return added;
    }

    /// <summary>
    /// 드롭 없이 가득 찰 때까지만 Add 하고 끝내는 경우
    /// 하나라도 overflow 발생 시 false
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="count"></param>
    public bool TryAddItemFromWorld(int itemID, int count)
    {
        var result = inventoryData.AddItemWithOverflow(itemID, count, out int overflow);
        uiService?.ShowItemAcquired(ItemDatabase.I.GetItem(itemID), result);
        return overflow == 0;
    }
    public bool CanUnequip(int itemID)
    {
        var testSlots = inventoryData.GetAllSlots();
        // 빈 슬롯이 있는지 체크하는 로직
        for (int i = 0; i < inventoryData.SlotCount; i++)
        {
            if (inventoryData.IsEmptySlot(i))
                return true;
        }

        uiService?.ShowWarning("인벤토리가 가득 찼습니다!");
        return false;
    }

    /// <summary>
    /// 아이템 사용 (소비/장착 통합)
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="amount">사용 수량 (기본 1)</param>
    /// <returns>사용 성공 여부</returns>
    public bool UseItem(int itemId, int amount = 1)
    {
        ItemBase item = ItemDatabase.I.GetItem(itemId);
        if (item == null) return false;

        // 소비 아이템
        if (item.canUseable)
        {
            // 1. 먼저 아이템 보유 확인
            if (!inventoryData.HasItem(itemId, amount))
                return false;

            // 2. 효과 적용 시도
            bool success = item.Apply(playerController);
            if (!success) return false;

            // 3. 성공 시에만 제거
            inventoryData.TryRemoveItem(itemId, amount);

            // 4. 빈 캔 생성 (특정 아이템)
            if (itemId == 4002001 || itemId == 4002002)
            {
                AddItemFromWorld(4102035, 1);
            }

            return true;
        }
        // 장착 아이템
        else if (item.canEquip)
        {
            return playerEquipmanager.ToggleEquip(itemId);
        }

        return false;
    }
    /// <summary>
    /// 도구 장착/해제 토글 (외부 호출용)
    /// </summary>
    public bool ToggleEquip(int itemId)
    {
        //equipmanager가 처리
        return playerEquipmanager.ToggleEquip(itemId);
    }
    public bool TryExpandWithChip(int tier)
    {
        return inventoryData.ExpandWithChip(tier);
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
    /// <summary>
    /// 외부 컨테이너에서 전체 가져오기
    /// </summary>
    public bool WithdrawAllFrom(IItemContainer source)
    {
        if (source == null || !source.HasItems)
            return false;

        var items = source.Items
            .Where(s => s != null && s.itemID > 0 && s.count > 0)
            .ToList();

        bool allTransferred = true;

        foreach (var slot in items)
        {
            int added = inventoryData.TryAddItem(slot.itemID, slot.count);
            if (added > 0)
            {
                source.TryRemoveItem(slot.itemID, added);
            }
            if (added < slot.count)
            {
                allTransferred = false;
            }
        }

        return allTransferred;
    }

    /// <summary>
    /// 외부 컨테이너로 전체 보내기
    /// </summary>
    public bool DepositAllTo(IItemContainer target)
    {
        if (target == null || !inventoryData.HasItems)
            return false;

        var items = inventoryData.Items
            .Where(s => s != null && s.itemID > 0 && s.count > 0)
            .ToList();

        bool allTransferred = true;

        foreach (var slot in items)
        {
            int added = target.TryAddItem(slot.itemID, slot.count);
            if (added > 0)
            {
                inventoryData.TryRemoveItem(slot.itemID, added);
            }
            if (added < slot.count)
            {
                allTransferred = false;
            }
        }

        return allTransferred;
    }
    #region UI 제어
    public void OpenInventory() {
        inventoryUI.ToggleInventory();
        TutorialEventBus.RaiseAction((int)TutorialActionId.OpenInventory);
    }
    public void CloseInventory() => inventoryUI.ToggleInventory();

    #endregion
}