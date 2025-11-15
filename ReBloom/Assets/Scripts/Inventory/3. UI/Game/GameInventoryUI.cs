using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 인벤토리 UI (아이콘 기반)
/// 테이블별 탭으로 분류하여 표시
/// </summary>
public class GameInventoryUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryUIRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private DebugItemTooltip tooltip;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnConsumable;
    [SerializeField] private Button btnProtective;
    [SerializeField] private Button btnTool;
    [SerializeField] private Button btnMisc;

    #region 상태 변수
    private ItemTableType currentTable = ItemTableType.Consumable;

    private List<DebugItemSlot> activeSlots = new List<DebugItemSlot>();
    private Dictionary<Button, ItemTableType> tabButtons = new Dictionary<Button, ItemTableType>();
    #endregion

    #region Unity 생명주기
    private void Awake()
    {
        InitializeTabButtons();
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
        inventoryData.OnMessage += ShowMessage;

        // 초기화
        inventoryData.Initialize();
        RefreshUI();

        // 시작 시 인벤토리 닫기
        inventoryUIRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inventoryData != null)
        {
            inventoryData.OnInventoryChanged -= RefreshUI;
            inventoryData.OnMessage -= ShowMessage;
        }
    }
    #endregion

    #region 초기화
    private void InitializeTabButtons()
    {
        // 테이블별 탭 매핑
        if (btnConsumable != null)
        {
            tabButtons[btnConsumable] = ItemTableType.Consumable;
            btnConsumable.onClick.AddListener(() => OnTabClicked(ItemTableType.Consumable));
        }

        if (btnProtective != null)
        {
            tabButtons[btnProtective] = ItemTableType.Protective;
            btnProtective.onClick.AddListener(() => OnTabClicked(ItemTableType.Protective));
        }

        if (btnTool != null)
        {
            tabButtons[btnTool] = ItemTableType.Tool;
            btnTool.onClick.AddListener(() => OnTabClicked(ItemTableType.Tool));
        }

        if (btnMisc != null)
        {
            tabButtons[btnMisc] = ItemTableType.Misc;
            btnMisc.onClick.AddListener(() => OnTabClicked(ItemTableType.Misc));
        }

        UpdateTabVisuals();
    }
    #endregion

    #region UI 이벤트
    public void ToggleInventory()
    {
        if (inventoryUIRoot == null) return;

        bool isActive = !inventoryUIRoot.activeSelf;
        inventoryUIRoot.SetActive(isActive);

        if (isActive)
        {
            RefreshUI();

            // 커서 표시
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // 게임 중에는 커서 숨김 (필요시)
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;
        }

        Debug.Log($"[게임 인벤토리] {(isActive ? "열림" : "닫힘")}");
    }

    private void OnTabClicked(ItemTableType tableType)
    {
        if (currentTable == tableType) return;

        currentTable = tableType;

        UpdateTabVisuals();
        RefreshUI();
    }
    #endregion

    #region UI 갱신
    /// <summary>
    /// 인벤토리 아이템 목록 새로고침
    /// </summary>
    public void RefreshUI()
    {
        if (inventoryData == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[GameInventoryUI] InventoryData 또는 ItemDatabase가 없습니다.");
            return;
        }

        // 기존 슬롯 제거
        ClearSlots();

        // 필터링된 아이템 가져오기
        var items = GetFilteredItems();

        // 슬롯 생성
        foreach (var itemPair in items)
        {
            int itemId = itemPair.Key;
            int quantity = itemPair.Value;

            ItemBase item = ItemDatabase.I.GetItem(itemId);
            if (item != null)
            {
                CreateItemSlot(item, quantity);
            }
        }
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
    }

    private void CreateItemSlot(ItemBase item, int quantity)
    {
        if (itemSlotPrefab == null || contentContainer == null)
        {
            Debug.LogError("[GameInventoryUI] itemSlotPrefab 또는 contentContainer가 없습니다!");
            return;
        }

        GameObject slotObj = Instantiate(itemSlotPrefab, contentContainer);
        DebugItemSlot slot = slotObj.GetComponent<DebugItemSlot>();

        if (slot != null)
        {
            // 슬롯 초기화
            slot.Initialize(item, tooltip);
            slot.SetShowDescription(false); // 게임 인벤토리는 간단하게
            slot.SetShowStats(false);

            // 수량 표시
             slot.SetQuantity(quantity);

            activeSlots.Add(slot);

            // 드래그 앤 드롭 핸들러 설정
            SetDragDropHandlerData(item, slot);
        }
    }

    private void SetDragDropHandlerData(ItemBase item, DebugItemSlot slot)
    {
        ItemIconDragHandler dragHandler = slot.GetComponent<ItemIconDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.SetItemData(item);
        }
    }
    #endregion

    #region 필터링
    /// <summary>
    /// 현재 선택된 테이블에 해당하는 아이템만 필터링
    /// </summary>
    private Dictionary<int, int> GetFilteredItems()
    {
        // 테이블별 필터링
        var filtered = new Dictionary<int, int>();

        foreach (var itemPair in inventoryData.Items)
        {
            int itemId = itemPair.Key;
            ItemTableType tableType = ItemIDParser.GetTableType(itemId);

            if (tableType == currentTable)
            {
                filtered.Add(itemId, itemPair.Value);
            }
        }

        return filtered;
    }
    #endregion

    #region UI 업데이트
    private void UpdateTabVisuals()
    {
        // 테이블별 탭 강조
        foreach (var pair in tabButtons)
        {
            Button btn = pair.Key;
            ItemTableType type = pair.Value;

            ColorBlock colors = btn.colors;
            colors.normalColor = (type == currentTable) ?
                new Color(0.3f, 0.6f, 1f) : Color.white;
            btn.colors = colors;

            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontStyle = (type == currentTable) ?
                    FontStyles.Bold : FontStyles.Normal;
            }
        }
    }
    #endregion

    #region 메시지 표시
    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.color = Color.yellow;

            // 3초 후 메시지 사라지게 (선택사항)
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 3f);
        }

        Debug.Log($"[인벤토리 메시지] {msg}");
    }

    private void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }
    #endregion
}