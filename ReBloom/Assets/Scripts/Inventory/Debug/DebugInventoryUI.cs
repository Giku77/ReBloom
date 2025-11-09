using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 런타임 디버그 인벤토리 UI
/// 빌드 후에도 테스트 가능
/// New Input System 사용
/// </summary>
public class DebugInventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private DebugItemTooltip tooltip;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnConsumable;
    [SerializeField] private Button btnProtective;
    [SerializeField] private Button btnTool;
    [SerializeField] private Button btnMisc;

    [Header("Filter UI")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Toggle toggleTierFilter;
    [SerializeField] private TMP_Dropdown dropdownTier;
    [SerializeField] private TMP_Dropdown dropdownSort;
    [SerializeField] private Button btnSortOrder;

    [Header("Display Options")]
    [SerializeField] private Toggle toggleShowDescription;
    [SerializeField] private Toggle toggleShowStats;
    [SerializeField] private Slider sliderIconSize;

    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI txtItemCount;
    [SerializeField] private TextMeshProUGUI txtFilterInfo;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference toggleInventoryAction;

    #region 상태 변수
    private ItemTableType currentTable = ItemTableType.Consumable;
    private ItemTier selectedTier = ItemTier.Common;
    private bool filterByTier = false;
    private string searchText = "";
    private SortOption currentSort = SortOption.ByID;
    private bool sortAscending = true;

    private List<DebugItemSlot> activeSlots = new List<DebugItemSlot>();
    private Dictionary<Button, ItemTableType> tabButtons = new Dictionary<Button, ItemTableType>();
    #endregion

    #region Unity 생명주기
    private void Awake()
    {
        InitializeTabButtons();
        InitializeFilterUI();

        uiRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // Input Action 활성화
        if (toggleInventoryAction != null)
        {
            toggleInventoryAction.action.Enable();
            toggleInventoryAction.action.performed += OnToggleInventory;
        }
    }

    private void OnDisable()
    {
        // Input Action 비활성화
        if (toggleInventoryAction != null)
        {
            toggleInventoryAction.action.performed -= OnToggleInventory;
            toggleInventoryAction.action.Disable();
        }
    }

    private void Start()
    {
        // ItemDatabase 초기화 대기
        if (ItemDatabase.I != null && ItemDatabase.I.IsInitialized)
        {
            RefreshItemList();
        }
        else
        {
            Debug.LogWarning("[DebugInventoryUI] ItemDatabase가 아직 초기화되지 않았습니다.");
        }
    }
    #endregion

    #region Input System 콜백
    /// <summary>
    /// F1 키 입력 콜백 (New Input System)
    /// </summary>
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        ToggleUI();
    }
    #endregion

    #region 초기화
    private void InitializeTabButtons()
    {
        tabButtons[btnConsumable] = ItemTableType.Consumable;
        tabButtons[btnProtective] = ItemTableType.Protective;
        tabButtons[btnTool] = ItemTableType.Tool;
        tabButtons[btnMisc] = ItemTableType.Misc;

        btnConsumable.onClick.AddListener(() => OnTabClicked(ItemTableType.Consumable));
        btnProtective.onClick.AddListener(() => OnTabClicked(ItemTableType.Protective));
        btnTool.onClick.AddListener(() => OnTabClicked(ItemTableType.Tool));
        btnMisc.onClick.AddListener(() => OnTabClicked(ItemTableType.Misc));

        UpdateTabVisuals();
    }

    private void InitializeFilterUI()
    {
        // 검색
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        // 티어 필터
        toggleTierFilter.onValueChanged.AddListener(OnTierFilterToggled);
        dropdownTier.ClearOptions();
        dropdownTier.AddOptions(new List<string> { "일반", "희귀", "영웅" });
        dropdownTier.onValueChanged.AddListener(OnTierChanged);

        // 정렬
        dropdownSort.ClearOptions();
        dropdownSort.AddOptions(new List<string> { "ID 순", "이름 순", "티어 순", "소분류 순" });
        dropdownSort.onValueChanged.AddListener(OnSortChanged);

        btnSortOrder.onClick.AddListener(OnSortOrderToggled);
        UpdateSortOrderButton();

        // 표시 옵션
        toggleShowDescription.onValueChanged.AddListener(_ => RefreshItemList());
        toggleShowStats.onValueChanged.AddListener(_ => RefreshItemList());
        sliderIconSize.onValueChanged.AddListener(OnIconSizeChanged);
    }
    #endregion

    #region UI 이벤트
    public void ToggleUI()
    {
        bool newState = !uiRoot.activeSelf;
        uiRoot.SetActive(newState);

        if (newState)
        {
            RefreshItemList();

            // 커서 표시 (빌드용)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnTabClicked(ItemTableType tableType)
    {
        if (currentTable == tableType) return;

        currentTable = tableType;
        searchInput.text = ""; // 탭 변경 시 검색 초기화

        UpdateTabVisuals();
        RefreshItemList();
    }

    private void OnSearchChanged(string text)
    {
        searchText = text;
        RefreshItemList();
    }

    private void OnTierFilterToggled(bool isOn)
    {
        filterByTier = isOn;
        dropdownTier.interactable = isOn;
        RefreshItemList();
    }

    private void OnTierChanged(int index)
    {
        selectedTier = (ItemTier)(index + 1);
        RefreshItemList();
    }

    private void OnSortChanged(int index)
    {
        currentSort = (SortOption)index;
        RefreshItemList();
    }

    private void OnSortOrderToggled()
    {
        sortAscending = !sortAscending;
        UpdateSortOrderButton();
        RefreshItemList();
    }

    private void OnIconSizeChanged(float size)
    {
        foreach (var slot in activeSlots)
        {
            slot.SetIconSize(size);
        }
    }
    #endregion

    #region 아이템 목록 갱신
    /// <summary>
    /// 필터링 및 정렬하여 아이템 목록 새로고침
    /// </summary>
    public void RefreshItemList()
    {
        if (ItemDatabase.I == null || !ItemDatabase.I.IsInitialized)
        {
            Debug.LogWarning("[DebugInventoryUI] ItemDatabase가 초기화되지 않음");
            return;
        }

        // 기존 슬롯 제거
        ClearSlots();

        // 필터링 & 정렬
        var items = GetFilteredAndSortedItems();

        // 슬롯 생성
        foreach (var item in items)
        {
            CreateItemSlot(item);
        }

        // 정보 업데이트
        UpdateInfoPanel(items.Count);
    }

    private void ClearSlots()
    {
        foreach (var slot in activeSlots)
        {
            Destroy(slot.gameObject);
        }
        activeSlots.Clear();
    }

    private void CreateItemSlot(ItemBase item)
    {
        GameObject slotObj = Instantiate(itemSlotPrefab, contentContainer);
        DebugItemSlot slot = slotObj.GetComponent<DebugItemSlot>();

        if (slot != null)
        {
            slot.Initialize(item, tooltip);
            slot.SetIconSize(sliderIconSize.value);
            slot.SetShowDescription(toggleShowDescription.isOn);
            slot.SetShowStats(toggleShowStats.isOn);

            activeSlots.Add(slot);
        }
    }
    #endregion

    #region 필터링 & 정렬
    private List<ItemBase> GetFilteredAndSortedItems()
    {
        var allItems = ItemDatabase.I.GetAllItems();

        // 1. 테이블 필터
        var filtered = allItems.Where(item =>
        {
            var tableType = ItemIDParser.GetTableType(item.itemID);
            return tableType == currentTable;
        }).ToList();

        // 2. 티어 필터
        if (filterByTier)
        {
            filtered = filtered.Where(item => item.tier == (int)selectedTier).ToList();
        }

        // 3. 검색 필터
        if (!string.IsNullOrEmpty(searchText))
        {
            string search = searchText.ToLower();
            filtered = filtered.Where(item =>
                item.itemName.ToLower().Contains(search) ||
                item.itemID.ToString().Contains(search)
            ).ToList();
        }

        // 4. 정렬
        filtered = SortItems(filtered);

        return filtered;
    }

    private List<ItemBase> SortItems(List<ItemBase> items)
    {
        IOrderedEnumerable<ItemBase> sorted = null;

        switch (currentSort)
        {
            case SortOption.ByID:
                sorted = sortAscending ?
                    items.OrderBy(item => item.itemID) :
                    items.OrderByDescending(item => item.itemID);
                break;

            case SortOption.ByName:
                sorted = sortAscending ?
                    items.OrderBy(item => item.itemName) :
                    items.OrderByDescending(item => item.itemName);
                break;

            case SortOption.ByTier:
                sorted = sortAscending ?
                    items.OrderBy(item => item.tier).ThenBy(item => item.itemID) :
                    items.OrderByDescending(item => item.tier).ThenBy(item => item.itemID);
                break;

            case SortOption.BySubCategory:
                sorted = sortAscending ?
                    items.OrderBy(item => ItemIDParser.GetSubCategory(item.itemID)).ThenBy(item => item.itemID) :
                    items.OrderByDescending(item => ItemIDParser.GetSubCategory(item.itemID)).ThenBy(item => item.itemID);
                break;
        }

        return sorted?.ToList() ?? items;
    }
    #endregion

    #region UI 업데이트
    private void UpdateTabVisuals()
    {
        foreach (var pair in tabButtons)
        {
            Button btn = pair.Key;
            ItemTableType type = pair.Value;

            ColorBlock colors = btn.colors;
            colors.normalColor = (type == currentTable) ? new Color(0.3f, 0.6f, 1f) : Color.white;
            btn.colors = colors;

            // 버튼 텍스트 업데이트
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontStyle = (type == currentTable) ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void UpdateSortOrderButton()
    {
        TextMeshProUGUI btnText = btnSortOrder.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.text = sortAscending ? "▲" : "▼";
        }
    }

    private void UpdateInfoPanel(int count)
    {
        if (txtItemCount != null)
        {
            txtItemCount.text = $"아이템: {count}개";
        }

        if (txtFilterInfo != null)
        {
            string info = $"테이블: {GetTableName(currentTable)}";

            if (filterByTier)
            {
                info += $" | 티어: {selectedTier}";
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                info += $" | 검색: \"{searchText}\"";
            }

            txtFilterInfo.text = info;
        }
    }
    #endregion

    #region 헬퍼
    private string GetTableName(ItemTableType type)
    {
        return type switch
        {
            ItemTableType.Consumable => "소비",
            ItemTableType.Protective => "보호구",
            ItemTableType.Tool => "도구",
            ItemTableType.Misc => "기타",
            _ => "알 수 없음"
        };
    }
    #endregion

    #region 디버그 명령어
    /// <summary>
    /// 콘솔 명령어로 열기 (빌드에서도 사용 가능)
    /// </summary>
    [ContextMenu("Open Debug Inventory")]
    public void OpenDebugInventory()
    {
        uiRoot.SetActive(true);
        RefreshItemList();
    }

    [ContextMenu("Close Debug Inventory")]
    public void CloseDebugInventory()
    {
        uiRoot.SetActive(false);
    }
    #endregion
}