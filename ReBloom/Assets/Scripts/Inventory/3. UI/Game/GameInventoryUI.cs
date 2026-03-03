using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 게임 인벤토리 UI (View)
/// UI 표시만 담당 - 비즈니스 로직은 GameInventory에서 처리
/// </summary>
public class GameInventoryUI : UIBase
{
    [Header("Controller Reference")]
    [SerializeField] private GameInventory gameInventory;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryUIRoot;
    //[SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private DebugItemTooltip tooltip;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private GameObject emptySlotPrefab;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnConsumable;
    [SerializeField] private Button btnProtective;
    [SerializeField] private Button btnMisc;
    [SerializeField] private Button btnImportant;

    [Header("Quickslot Fail Image")]
    [SerializeField] private GameObject deActiveGameObject;

    [Header("TrashBin Image")]
    [SerializeField] private GameObject removeGradientGameObject;

    [Header("UI 임시정리")]
    [SerializeField] private GameObject gameEquipIcon;
    [SerializeField] private GameObject gameQuickSlotRoot;
    [SerializeField] private GameObject gamePlayerInfoRoot;

    private QuestUI questUI;

    private List<Transform> emptySlotList;
    private List<Transform> lockSlotList;
    private int lastTotalSlotCount = -1;

    #region 상태 변수
    private InventorySlotType currentType = InventorySlotType.Consumable;
    private readonly List<GameInventorySlot> activeSlots = new();
    private readonly Dictionary<Button, InventorySlotType> tabButtons = new();
    #endregion

    #region Unity 생명주기
    protected override void Awake()
    {
        base.Awake();

        InitializeTabButtons();
        questUI = FindFirstObjectByType<QuestUI>();

        if (emptySlotList == null)
        {
            emptySlotList = new List<Transform>();
        }

        if (lockSlotList == null)
        {
            lockSlotList = new List<Transform>();
        }
    }

    private void Start()
    {
        if (gameInventory == null)
        {
            Debug.LogError("[GameInventoryUI] gameInventory가 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        DragDropManager.OnDragFeedback += HandleDragFeedback;
        DragDropManager.OnDragFeedback += HandleTrashFeedback;
    }

    private void OnEnable()
    {
        InventroyEventSystem.InventoryOpened();
        removeGradientGameObject.SetActive(false);

        if (gameInventory != null)
        {
            gameInventory.OnInventoryChanged += RefreshUI;
            gameInventory.OnInventoryBound += RefreshUI;
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
        {
            Debug.LogWarning("[GameInventoryUI] EventSystem이 활성화되어 있지 않습니다.");
        }
        else
        {
            tabButtons.All(pair =>
            {
                if (pair.Value == currentType)
                {
                    currentEventSystem.SetSelectedGameObject(pair.Key.gameObject);
                    OnTabClicked(currentType);
                    return false;
                }
                return true;
            });
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (gameInventory != null)
        {
            gameInventory.OnInventoryChanged -= RefreshUI;
            gameInventory.OnInventoryBound -= RefreshUI;
        }

        InventroyEventSystem.InventoryClosed();
    }

    private void OnDestroy()
    {
        DragDropManager.OnDragFeedback -= HandleDragFeedback;
        DragDropManager.OnDragFeedback -= HandleTrashFeedback;
    }
    #endregion

    #region 초기화
    private void InitializeTabButtons()
    {
        if (btnConsumable != null)
        {
            tabButtons[btnConsumable] = InventorySlotType.Consumable;
            btnConsumable.onClick.AddListener(() => OnTabClicked(InventorySlotType.Consumable));
        }

        if (btnProtective != null)
        {
            tabButtons[btnProtective] = InventorySlotType.Equipment;
            btnProtective.onClick.AddListener(() => OnTabClicked(InventorySlotType.Equipment));
        }

        if (btnMisc != null)
        {
            tabButtons[btnMisc] = InventorySlotType.Misc;
            btnMisc.onClick.AddListener(() => OnTabClicked(InventorySlotType.Misc));
        }

        if (btnImportant != null)
        {
            tabButtons[btnImportant] = InventorySlotType.Important;
            btnImportant.onClick.AddListener(() => OnTabClicked(InventorySlotType.Important));
        }

        UpdateTabVisuals();
    }
    #endregion

    #region UI 이벤트
    public void ToggleInventory()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
          return;
        var type = PlatformManager.Instance?.IsMobile == true
            ? UIType.MobileInventory
            : UIType.Inventory;
        UIManager.Instance?.ToggleUI(type);
    }
    protected override void OnShow()
    {
        RefreshUI();
        SoundManager.I?.PlayOpenInventory();
    }

    protected override void OnHide()
    {
        SoundManager.I?.PlayCloseInventory();
    }

    private void OnTabClicked(InventorySlotType inventoryType)
    {
        if (currentType == inventoryType) return;

        currentType = inventoryType;

        UpdateTabVisuals();
        RefreshUI();
    }
    /// <summary>
    /// 인벤토리 빈슬롯 생성 (기본 슬롯 + 잠긴 슬롯)
    /// </summary>
    public void CreateEmptySlots()
    {
        if (contentContainer == null)
        {
            Debug.LogError("[GameInventoryUI] contentContainer가 할당되지 않았습니다!");
            return;
        }

        if (gameInventory == null)
        {
            Debug.LogWarning("[GameInventoryUI] gameInventory가 없어 슬롯 생성 불가");
            return;
        }

        int baseSlots = gameInventory.SlotCount;
        int lockSlots = gameInventory.LockedSlotCount;
        int totalRequiredSlots = baseSlots + lockSlots;

        //Debug.Log($"[GameInventoryUI] 슬롯 체크 - Tier: {inventoryData.InventoryTier}, " + $"기본: {baseSlots}, 잠금: {lockSlots}, 총: {totalRequiredSlots}");
        bool slotStructureChanged = lastTotalSlotCount != totalRequiredSlots;

        // ===== 2단계: 티어가 변경되었으면 잠금 슬롯 제거 =====
        if (slotStructureChanged && lockSlotList.Count > 0)
        {
            foreach (var lockSlot in lockSlotList)
            {
                if (lockSlot != null)
                {
                    emptySlotList.Remove(lockSlot);
                    Destroy(lockSlot.gameObject);
                }
            }
            lockSlotList.Clear();
        }

        // 현재 티어 저장
        lastTotalSlotCount = totalRequiredSlots;

        // ===== 3단계: 기본 슬롯 추가 (부족한 만큼만) =====
        int currentBaseSlots = emptySlotList.Count; // 현재는 기본 슬롯만 있음

        if (currentBaseSlots < baseSlots)
        {
            int slotsToAdd = baseSlots - currentBaseSlots;
            Debug.Log($"[GameInventoryUI] 기본 슬롯 {slotsToAdd}개 추가 중...");

            for (int i = 0; i < slotsToAdd; i++)
            {
                int globalIndex = currentBaseSlots + i;

                var slotInstance = Instantiate(emptySlotPrefab);
                slotInstance.transform.SetParent(contentContainer, false);
                slotInstance.transform.localScale = Vector3.one;
                slotInstance.name = $"EmptySlot_{globalIndex}";

                var dropZoneMarker = slotInstance.GetComponent<DropZoneMarker>();
                if (dropZoneMarker != null)
                {
                    dropZoneMarker.SetZoneType(DropZoneType.Inventory);
                    dropZoneMarker.SetSlotIndex(globalIndex); // 동적 인덱스 설정
                    dropZoneMarker.SetPriority(50);
                }
                else
                {
                    Debug.LogWarning($"[GameInventoryUI] EmptySlot_{globalIndex}에 DropZoneMarker가 없습니다!");
                }
                // 마커 비활성화 (기본 슬롯)
                var deactivateMarker = slotInstance.GetComponentInChildren<DeactivateSlotMarker>(true);
                var lockMarker = slotInstance.GetComponentInChildren<LockImageMarker>(true);

                if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
                if (lockMarker != null) lockMarker.gameObject.SetActive(false);

                emptySlotList.Add(slotInstance.transform);
                //Debug.Log($"[GameInventoryUI] 기본 슬롯 생성 (인덱스: {globalIndex})");
            }
        }

        // ===== 4단계: 잠금 슬롯 추가 =====
        if (lockSlots > 0 && lockSlotList.Count == 0)
        {
            Debug.Log($"[GameInventoryUI] 잠금 슬롯 {lockSlots}개 추가 중...");

            for (int i = 0; i < lockSlots; i++)
            {
                int globalIndex = baseSlots + i;

                var slotInstance = Instantiate(emptySlotPrefab);
                slotInstance.transform.SetParent(contentContainer, false);
                slotInstance.transform.localScale = Vector3.one;
                slotInstance.name = $"LockSlot_{i}";

                var dropZoneMarker = slotInstance.GetComponent<DropZoneMarker>();
                if (dropZoneMarker != null)
                {
                    // 잠금 슬롯은 드롭존 비활성화 또는 우선순위 낮게
                    dropZoneMarker.SetZoneType(DropZoneType.Inventory);
                    dropZoneMarker.SetSlotIndex(globalIndex);
                    dropZoneMarker.SetPriority(-1); // 드롭 불가
                }

                var deactivateMarker = slotInstance.GetComponentInChildren<DeactivateSlotMarker>(true);
                var lockMarker = slotInstance.GetComponentInChildren<LockImageMarker>(true);

                // 일단 둘 다 비활성화
                if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
                if (lockMarker != null) lockMarker.gameObject.SetActive(false);

                // 첫 번째 잠금 슬롯: LockImageMarker 활성화
                if (i == 0)
                {
                    if (lockMarker != null)
                    {
                        lockMarker.gameObject.SetActive(true);
                        //Debug.Log($"[GameInventoryUI] LockImageMarker 활성화 (슬롯 {globalIndex})");
                    }
                }
                // 나머지 잠금 슬롯: DeactivateSlotMarker 활성화
                else
                {
                    if (deactivateMarker != null)
                    {
                        deactivateMarker.gameObject.SetActive(true);
                        //Debug.Log($"[GameInventoryUI] DeactivateSlotMarker 활성화 (슬롯 {globalIndex})");
                    }
                }

                lockSlotList.Add(slotInstance.transform);
                emptySlotList.Add(slotInstance.transform);
            }
        }

       // Debug.Log($"[GameInventoryUI] 슬롯 생성 완료 - 기본: {baseSlots}개, 잠금: {lockSlotList.Count}개, 총: {emptySlotList.Count}개");
    }

    /// <summary>
    /// 인벤토리 아이템 목록 새로고침 (슬롯 기반)
    /// </summary>
    public void RefreshUI()
    {
        if (gameInventory == null)
        {
            Debug.LogWarning("[GameInventoryUI] gameInventory가 null이라 RefreshUI 중단");
            return;
        }

        CreateEmptySlots();
        ClearSlots();

        var slots = gameInventory.GetAllSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemID > 0)
            {
                ItemBase item = ItemDatabase.I.GetItem(slots[i].itemID);
                if (item != null)
                {
                    CreateItemSlot(item, slots[i].count, i);
                }
            }
        }

        questUI?.Refresh();
        QuestManager.I?.PlayQuestCompleteAnimation();
    }

    private void ClearSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        activeSlots.Clear();

        //Debug.Log($"[GameInventoryUI] 아이템 슬롯 정리 완료");
    }

    private void CreateItemSlot(ItemBase item, int quantity, int slotIndex)
    {
        if (slotIndex >= emptySlotList.Count)
        {
            Debug.LogError($"[GameInventoryUI] 슬롯 인덱스 초과: {slotIndex}");
            return;
        }

        GameObject slotObj = Instantiate(itemSlotPrefab, emptySlotList[slotIndex]);

        if (!slotObj.TryGetComponent(out GameInventorySlot slot))
        {
            Debug.LogError("[GameInventoryUI] GameInventorySlot 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // IItemSlot 인터페이스 메서드 사용
        slot.SetItem(item, quantity);
        // 드래그 핸들러 데이터 설정
        SetDragDropHandlerData(item, slot);
        activeSlots.Add(slot);
    }

    private void SetDragDropHandlerData(ItemBase item, GameInventorySlot slot)
    {
        if (!slot.TryGetComponent(out ItemIconDragHandler dragHandler))
        {
            Debug.LogError("[GameInventoryUI] ItemIconDragHandler 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        dragHandler.SetItemData(item);
    }
    #endregion

    #region 드래그 피드백
    /// <summary>
    /// 드래그 중 전역 피드백 처리
    /// </summary>
    private void HandleDragFeedback(DragContext context, DropZoneMarker zone, bool canDrop)
    {
        if (deActiveGameObject == null) return;

        // 드래그 중이 아니면 숨김
        if (context == null)
        {
            deActiveGameObject.SetActive(false);
            return;
        }

        // 퀵슬롯에 배치할 수 없는 아이템인 경우에만 경고 표시
        bool shouldShowWarning = !context.Item.canQuickSlot;

        deActiveGameObject.SetActive(shouldShowWarning);

        // 디버깅용 로그 (선택사항)
        //if (shouldShowWarning)
        //{
        //    Debug.Log($"[GameInventoryUI] 퀵슬롯 불가 경고: {context.Item.itemName}");
        //}
    }

    /// <summary>
    /// 드래그 중 전역 피드백 처리
    /// </summary>
    private void HandleTrashFeedback(DragContext context, DropZoneMarker zone, bool canDrop)
    {
        if (removeGradientGameObject == null) return;

        // 드래그 중이 아니면 숨김
        if (context == null)
        {
            removeGradientGameObject.SetActive(false);
            return;
        }

        if (zone == null) { return; }

        // trashbinzone인 경우에만 패널 표시
        if(zone != null)
        {
            bool shouldShowTrash = zone.ZoneType == DropZoneType.TrashBin;
            //Debug.Log($"[GameInventroyUI] {removeGradientGameObject}");
            removeGradientGameObject.SetActive(shouldShowTrash);
        }
    }
    #endregion

    #region UI 업데이트
    private void UpdateTabVisuals()
    {
        foreach (var pair in tabButtons)
        {
            Button btn = pair.Key;
            InventorySlotType type = pair.Value;

            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontStyle = (type == currentType) ?
                    FontStyles.Bold : FontStyles.Normal;

                Color textColor = btnText.color;
                textColor.a = (type == currentType) ? 1f : 0.5f;
                btnText.color = textColor;
            }
        }
    }
    #endregion
}