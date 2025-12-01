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

    [Header("UI 임시정리")]
    [SerializeField] private GameObject gameEquipIcon;
    [SerializeField] private GameObject gameQuickSlotRoot;
    [SerializeField] private GameObject gamePlayerInfoRoot;

    private QuestUI questUI;

    private List<Transform> emptySlotList;
    private List<Transform> lockSlotList;
    private int lastTier = -1;

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
        if (inventoryData == null)
        {
            Debug.LogError("[GameInventoryUI] InventoryData가 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        // 이벤트 구독
        inventoryData.OnInventoryChanged += RefreshUI;

        ////// 초기화
        //inventoryData.Initialize();
        //CreateEmptySlots();
        //RefreshUI();
        // 시작 시 인벤토리 닫기
        //inventoryUIRoot.SetActive(false);
    }

    private void Update()
    {
        deActiveGameObject.SetActive(false);
        bool success = ItemIconDragHandler.CurrentContext?.Item.canQuickSlot ?? false;
        if (!success && ItemIconDragHandler.CurrentContext?.Item != null)
        {
            deActiveGameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        //인벤토리 UI가 열릴 때 정적 이벤트 발생
        InventroyEventSystem.InventoryOpened();

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
    }

    private void OnDisable()
    {
        //인벤토리 UI가 닫힐 때 정적 이벤트 발생
        InventroyEventSystem.InventoryClosed();
    }

    private void OnDestroy()
    {
        if (inventoryData != null)
        {
            inventoryData.OnInventoryChanged -= RefreshUI;
        }
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
        //if (inventoryUIRoot == null) return;

        //bool isActive = !inventoryUIRoot.activeSelf;
        //inventoryUIRoot.SetActive(isActive);

        //if (isActive)
        //{
        //    RefreshUI();

        //    // 커서 표시
        //    Cursor.visible = true;
        //    Cursor.lockState = CursorLockMode.None;
        //    Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = true;
        //}
        //else
        //{
        //    // 게임 중에는 커서 숨김
        //    Cursor.visible = false;
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = false;
        //}
        UIManager.Instance?.ToggleUI(Type);
        //UIManager.Instance?.ToggleUI(UIType.InventoryStats);

        //Debug.Log($"[게임 인벤토리] {(isActive ? "열림" : "닫힘")}");
    }
    protected override void OnShow()
    {
       RefreshUI();
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

        // ===== 1단계: 현재 필요한 총 슬롯 개수 계산 =====
        int baseSlots = inventoryData.SlotCount; // Tier에 따라 10/20/30/40
        int lockSlots = (inventoryData.InventoryTier < 3) ? 5 : 0;
        int totalRequiredSlots = baseSlots + lockSlots;

        Debug.Log($"[GameInventoryUI] 슬롯 체크 - Tier: {inventoryData.InventoryTier}, " +
                  $"기본: {baseSlots}, 잠금: {lockSlots}, 총: {totalRequiredSlots}");

        // ===== 2단계: 티어가 변경되었으면 잠금 슬롯 제거 =====
        if (lastTier != inventoryData.InventoryTier && lockSlotList.Count > 0)
        {
            Debug.Log($"[GameInventoryUI] 티어 변경 감지 ({lastTier} -> {inventoryData.InventoryTier}), 기존 잠금 슬롯 제거");

            foreach (var lockSlot in lockSlotList)
            {
                if (lockSlot != null)
                {
                    emptySlotList.Remove(lockSlot); // emptySlotList에서도 제거
                    Destroy(lockSlot.gameObject);
                }
            }
            lockSlotList.Clear();
        }

        // 현재 티어 저장
        lastTier = inventoryData.InventoryTier;

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

                // 마커 비활성화 (기본 슬롯)
                var deactivateMarker = slotInstance.GetComponentInChildren<DeactivateSlotMarker>(true);
                var lockMarker = slotInstance.GetComponentInChildren<LockImageMarker>(true);

                if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
                if (lockMarker != null) lockMarker.gameObject.SetActive(false);

                emptySlotList.Add(slotInstance.transform);
                Debug.Log($"[GameInventoryUI] 기본 슬롯 생성 (인덱스: {globalIndex})");
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

        Debug.Log($"[GameInventoryUI] 슬롯 생성 완료 - 기본: {baseSlots}개, 잠금: {lockSlotList.Count}개, 총: {emptySlotList.Count}개");
    }

    /// <summary>
    /// 인벤토리 아이템 목록 새로고침 (슬롯 기반)
    /// </summary>
    public void RefreshUI()
    {
        if (gameInventory == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[GameInventoryUI] GameInventory 또는 ItemDatabase가 없습니다.");
            return;
        }

        // 슬롯 개수 업데이트
        CreateEmptySlots();

        // 기존 아이템 슬롯 제거
        ClearSlots();

        // 슬롯 리스트를 직접 가져오기
        var slots = gameInventory.GetAllSlots();

        // 아이템 슬롯 생성
        int slotIndex = 0;
        foreach (var slot in slots)
        {
            ItemBase item = ItemDatabase.I.GetItem(slot.itemID);
            if (item != null)
            {
                CreateItemSlot(item, slot.count, slotIndex);
                slotIndex++;
            }
        }

        // 퀘스트 UI 갱신
        questUI?.Refresh();
        QuestManager.I?.PlayQuestCompleteAnimation();

        Debug.Log($"[GameInventoryUI] UI 갱신 완료 - Tier {inventoryData.InventoryTier}, 슬롯: {emptySlotList.Count}, 아이템: {slotIndex}");
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
        if (itemSlotPrefab == null || emptySlotList == null || slotIndex >= emptySlotList.Count)
        {
            Debug.LogError($"{itemSlotPrefab}:itemSlotPrefab / {emptySlotList}:emptySlotList / {slotIndex >= emptySlotList.Count}:slotIndex >= emptySlotList.Count ");
            Debug.LogError("[GameInventoryUI] itemSlotPrefab 또는 emptySlotList가 없거나 슬롯 인덱스 초과!");
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