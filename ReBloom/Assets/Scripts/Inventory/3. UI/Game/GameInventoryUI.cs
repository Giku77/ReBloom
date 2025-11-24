using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 게임 인벤토리 UI (View)
/// UI 표시만 담당 - 비즈니스 로직은 GameInventory에서 처리
/// </summary>
public class GameInventoryUI : MonoBehaviour
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
    [SerializeField] private List<Transform> emptySlotList;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnConsumable;
    [SerializeField] private Button btnProtective;
    [SerializeField] private Button btnMisc;
    [SerializeField] private Button btnImportant;

    private QuestUI questUI;

    #region 상태 변수
    private InventorySlotType currentType = InventorySlotType.Consumable;
    private readonly List<DebugItemSlot> activeSlots = new();
    private readonly Dictionary<Button, InventorySlotType> tabButtons = new();
    #endregion

    #region Unity 생명주기
    private void Awake()
    {
        InitializeTabButtons();
        questUI = FindFirstObjectByType<QuestUI>();
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

        // 초기화
        inventoryData.Initialize();
        RefreshUI();

        // 시작 시 인벤토리 닫기
        inventoryUIRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
        {
            Debug.LogWarning("[GameInventoryUI] EventSystem이 활성화되어 있지 않습니다.");
        }
        else
        {
            //tabButtons.All(pair =>
            //{
            //    if (pair.Value == currentType)
            //    {
            //        currentEventSystem.SetSelectedGameObject(pair.Key.gameObject);
            //        OnTabClicked(currentType);
            //        return false; // 중단
            //    }
            //    return true; // 계속
            //});
        }
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
            // 게임 중에는 커서 숨김
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        //Debug.Log($"[게임 인벤토리] {(isActive ? "열림" : "닫힘")}");
    }

    private void OnTabClicked(InventorySlotType inventoryType)
    {
        if (currentType == inventoryType) return;

        currentType = inventoryType;

        UpdateTabVisuals();
        RefreshUI();
    }
    #endregion

    #region UI 갱신
    /// <summary>
    /// 인벤토리 아이템 목록 새로고침
    /// 컨트롤러에서 필터링된 데이터를 받아서 표시만 함
    /// </summary>
    public void RefreshUI()
    {
        if (gameInventory == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[GameInventoryUI] GameInventory 또는 ItemDatabase가 없습니다.");
            return;
        }

        // 기존 슬롯 제거
        ClearSlots();

        // 컨트롤러에서 필터링된 아이템 가져오기
        //var items = gameInventory.GetSortedItems(currentType);
        var items = gameInventory.GetSortedItems();

        // 슬롯 생성
        int slotIndex = 0;
        foreach (var itemPair in items)
        {
            int itemId = itemPair.Key;
            int quantity = itemPair.Value;

            ItemBase item = ItemDatabase.I.GetItem(itemId);
            if (item != null)
            {
                CreateItemSlot(item, quantity, slotIndex);
                slotIndex++;
            }
        }
        questUI.Refresh();
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

        foreach (var emptySlot in emptySlotList)
        {
            if (emptySlot != null)
            {
                foreach (Transform child in emptySlot)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void CreateItemSlot(ItemBase item, int quantity, int slotIndex)
    {
        if (itemSlotPrefab == null || emptySlotList == null || slotIndex >= emptySlotList.Count)
        {
            Debug.LogError("[GameInventoryUI] itemSlotPrefab 또는 emptySlotList가 없거나 슬롯 인덱스 초과!");
            return;
        }

        GameObject slotObj = Instantiate(itemSlotPrefab, emptySlotList[slotIndex]);
        if (!slotObj.TryGetComponent(out DebugItemSlot slot))
        {
            Debug.LogError("[GameInventoryUI] DebugItemSlot 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        slot.Initialize(item, tooltip);
        slot.SetShowDescription(false);
        slot.SetShowStats(false);
        slot.SetQuantity(quantity);

        activeSlots.Add(slot);

        SetDragDropHandlerData(item, slot);
    }

    private void SetDragDropHandlerData(ItemBase item, DebugItemSlot slot)
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