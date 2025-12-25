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
    public enum PlatformTarget { Both, PCOnly, MobileOnly }

    [Header("Platform Target")]
    [SerializeField] private PlatformTarget targetPlatform = PlatformTarget.Both;

    [Header("Platform UI")]
    [SerializeField] private GameObject pcUIRoot;      // PC용 UI 루트
    [SerializeField] private GameObject mobileUIRoot;  // 모바일용 UI 루트
    [Header("Platform Content Containers")]
    [SerializeField] private Transform pcContentContainer;      // PC용 슬롯 컨테이너
    [SerializeField] private Transform mobileContentContainer;

    [Header("Controller Reference")]
    [SerializeField] private GameInventory gameInventory;

    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

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

    #region 플랫폼 체크
    /// <summary>
    /// 이 UI가 현재 플랫폼에서 활성화되어야 하는지
    /// </summary>
    private bool IsActiveForCurrentPlatform()
    {
        if (PlatformManager.Instance == null) return true;

        return targetPlatform switch
        {
            PlatformTarget.PCOnly => PlatformManager.Instance.IsPC,
            PlatformTarget.MobileOnly => PlatformManager.Instance.IsMobile,
            PlatformTarget.Both => true,
            _ => true
        };
    }

    private GameObject ActiveUIRoot => PlatformManager.Instance.IsMobile? mobileUIRoot: pcUIRoot;

    private Transform ActiveContentContainer => PlatformManager.Instance.IsMobile? mobileContentContainer: pcContentContainer;
    #endregion

    #region Unity 생명주기
    protected override void Awake()
    {
        // 현재 플랫폼용이 아니면 비활성화
        emptySlotList ??= new List<Transform>();
        lockSlotList ??= new List<Transform>();

        base.Awake();

        if (!IsActiveForCurrentPlatform())
        {
            enabled = false;  // 스크립트만 비활성화
            // gameObject.SetActive(false);
            return;
        }

        InitializeTabButtons();
        questUI = FindFirstObjectByType<QuestUI>();
    }

    private void Start()
    {
        if (!IsActiveForCurrentPlatform()) return;

        if (inventoryData == null)
        {
            Debug.LogError("[GameInventoryUI] InventoryData가 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        DragDropManager.OnDragFeedback += HandleDragFeedback;
        DragDropManager.OnDragFeedback += HandleTrashFeedback;
    }

    private void OnEnable()
    {
        if (!IsActiveForCurrentPlatform()) return;

        InventroyEventSystem.InventoryOpened();

        if (removeGradientGameObject != null)
            removeGradientGameObject.SetActive(false);

        if (inventoryData != null)
            inventoryData.OnContainerChanged += RefreshUI;

        // 탭 선택
        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem != null)
        {
            foreach (var pair in tabButtons)
            {
                if (pair.Value == currentType)
                {
                    currentEventSystem.SetSelectedGameObject(pair.Key.gameObject);
                    UpdateTabVisuals();
                    break;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (!IsActiveForCurrentPlatform()) return;

        if (inventoryData != null)
            inventoryData.OnContainerChanged -= RefreshUI;

        InventroyEventSystem.InventoryClosed();
    }

    private void OnDestroy()
    {
        if (!IsActiveForCurrentPlatform()) return;

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
        if (!IsActiveForCurrentPlatform()) return;

        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        UIManager.Instance?.ToggleUI(Type);
    }
    protected override void OnShow()
    {
        Debug.Log($"[GameInventoryUI] OnShow 호출됨 - {gameObject.name}");

        if (pcUIRoot != null)
            pcUIRoot.SetActive(PlatformManager.Instance.IsPC);

        if (mobileUIRoot != null)
            mobileUIRoot.SetActive(PlatformManager.Instance.IsMobile);

        ClearAllSlots();

        RefreshUI();
        SoundManager.I?.PlayOpenInventory();
    }

    protected override void OnHide()
    {
        pcUIRoot?.SetActive(false);
        mobileUIRoot?.SetActive(false);
        SoundManager.I?.PlayCloseInventory();
    }

    private void OnTabClicked(InventorySlotType inventoryType)
    {
        if (currentType == inventoryType) return;

        currentType = inventoryType;

        UpdateTabVisuals();
        RefreshUI();
    }
    private void ClearAllSlots()
    {
        // 아이템 슬롯 제거
        if (activeSlots != null)
        {
            foreach (var slot in activeSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            activeSlots.Clear();
        }

        if (emptySlotList != null)
        {
            foreach (var emptySlot in emptySlotList)
            {
                if (emptySlot != null) Destroy(emptySlot.gameObject);
            }
            emptySlotList.Clear();
        }

        lockSlotList?.Clear();
        lastTotalSlotCount = -1;
    }

    /// <summary>
    /// 인벤토리 빈슬롯 생성 (기본 슬롯 + 잠긴 슬롯)
    /// </summary>
    public void CreateEmptySlots()
    {
        Debug.Log($"[GameInventoryUI] CreateEmptySlots 시작");
        Debug.Log($"[GameInventoryUI] ActiveContentContainer: {ActiveContentContainer?.name ?? "NULL"}");

        if (ActiveContentContainer == null)
        {
            Debug.LogError("[GameInventoryUI] ActiveContentContainer가 NULL!");
            return;
        }

        int baseSlots = inventoryData.SlotCount;
        int lockSlots = inventoryData.LockedSlotCount;
        int totalRequiredSlots = baseSlots + lockSlots;

        Debug.Log($"[GameInventoryUI] baseSlots: {baseSlots}, lockSlots: {lockSlots}, total: {totalRequiredSlots}");
        Debug.Log($"[GameInventoryUI] lastTotalSlotCount: {lastTotalSlotCount}, emptySlotList.Count: {emptySlotList.Count}");

        // 슬롯 개수가 같으면 생성 스킵
        if (lastTotalSlotCount == totalRequiredSlots && emptySlotList.Count == totalRequiredSlots)
        {
            Debug.Log($"[GameInventoryUI] 슬롯 개수 동일 - 스킵!");
            return;
        }

        bool slotStructureChanged = lastTotalSlotCount != totalRequiredSlots;

        // 티어 변경 시 잠금 슬롯만 제거
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

        lastTotalSlotCount = totalRequiredSlots;

        // 기본 슬롯 개수 = 전체 - 잠금 슬롯
        int currentBaseSlots = emptySlotList.Count - lockSlotList.Count;

        // 기본 슬롯 추가 (부족한 만큼만)
        if (currentBaseSlots < baseSlots)
        {
            int slotsToAdd = baseSlots - currentBaseSlots;

            for (int i = 0; i < slotsToAdd; i++)
            {
                int globalIndex = currentBaseSlots + i;

                var slotInstance = Instantiate(emptySlotPrefab);
                slotInstance.transform.SetParent(ActiveContentContainer, false);
                slotInstance.transform.localScale = Vector3.one;
                slotInstance.name = $"EmptySlot_{globalIndex}";

                var dropZoneMarker = slotInstance.GetComponent<DropZoneMarker>();
                if (dropZoneMarker != null)
                {
                    dropZoneMarker.SetZoneType(DropZoneType.Inventory);
                    dropZoneMarker.SetSlotIndex(globalIndex);
                    dropZoneMarker.SetPriority(50);
                }

                var deactivateMarker = slotInstance.GetComponentInChildren<DeactivateSlotMarker>(true);
                var lockMarker = slotInstance.GetComponentInChildren<LockImageMarker>(true);

                if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
                if (lockMarker != null) lockMarker.gameObject.SetActive(false);

                emptySlotList.Add(slotInstance.transform);
            }
        }

        // 잠금 슬롯 추가
        if (lockSlots > 0 && lockSlotList.Count == 0)
        {
            for (int i = 0; i < lockSlots; i++)
            {
                int globalIndex = baseSlots + i;

                var slotInstance = Instantiate(emptySlotPrefab);
                slotInstance.transform.SetParent(ActiveContentContainer, false);
                slotInstance.transform.localScale = Vector3.one;
                slotInstance.name = $"LockSlot_{i}";

                var dropZoneMarker = slotInstance.GetComponent<DropZoneMarker>();
                if (dropZoneMarker != null)
                {
                    dropZoneMarker.SetZoneType(DropZoneType.Inventory);
                    dropZoneMarker.SetSlotIndex(globalIndex);
                    dropZoneMarker.SetPriority(-1);
                }

                var deactivateMarker = slotInstance.GetComponentInChildren<DeactivateSlotMarker>(true);
                var lockMarker = slotInstance.GetComponentInChildren<LockImageMarker>(true);

                if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
                if (lockMarker != null) lockMarker.gameObject.SetActive(false);

                if (i == 0)
                {
                    if (lockMarker != null) lockMarker.gameObject.SetActive(true);
                }
                else
                {
                    if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(true);
                }

                lockSlotList.Add(slotInstance.transform);
                emptySlotList.Add(slotInstance.transform);
            }
        }
    }

    /// <summary>
    /// 인벤토리 아이템 목록 새로고침 (슬롯 기반)
    /// </summary>
    public void RefreshUI()
    {
        Debug.Log($"[GameInventoryUI] RefreshUI 시작 - {gameObject.name}");
        Debug.Log($"[GameInventoryUI] IsActiveForCurrentPlatform: {IsActiveForCurrentPlatform()}");
        Debug.Log($"[GameInventoryUI] activeInHierarchy: {gameObject.activeInHierarchy}");

        if (!IsActiveForCurrentPlatform())
        {
            Debug.Log($"[GameInventoryUI] 플랫폼 체크 실패로 스킵");
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.Log($"[GameInventoryUI] 비활성화 상태라 스킵");
            return;
        }

        Debug.Log($"[GameInventoryUI] CreateEmptySlots 호출 전");
        CreateEmptySlots();
        Debug.Log($"[GameInventoryUI] CreateEmptySlots 완료 - emptySlotList.Count: {emptySlotList.Count}");

        ClearSlots();

        var slots = inventoryData.GetAllSlots();
        Debug.Log($"[GameInventoryUI] inventoryData 슬롯 개수: {slots.Length}");

        for (int i = 0; i < slots.Length; i++)
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

        Debug.Log($"[GameInventoryUI] RefreshUI 완료 - activeSlots.Count: {activeSlots.Count}");
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