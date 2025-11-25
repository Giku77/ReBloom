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
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private GameObject emptySlotPrefab;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnConsumable;
    [SerializeField] private Button btnProtective;
    [SerializeField] private Button btnMisc;
    [SerializeField] private Button btnImportant;

    private QuestUI questUI;

    private List<Transform> emptySlotList;

    #region 상태 변수
    private InventorySlotType currentType = InventorySlotType.Consumable;
    private readonly List<GameInventorySlot> activeSlots = new();
    private readonly Dictionary<Button, InventorySlotType> tabButtons = new();
    #endregion

    #region Unity 생명주기
    private void Awake()
    {
        InitializeTabButtons();
        questUI = FindFirstObjectByType<QuestUI>();
        if (emptySlotList == null)
        {
            emptySlotList = new List<Transform>();
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

        // 초기화
        inventoryData.Initialize();
        CreateEmptySlots();
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
            tabButtons.All(pair =>
            {
                if (pair.Value == currentType)
                {
                    currentEventSystem.SetSelectedGameObject(pair.Key.gameObject);
                    OnTabClicked(currentType);
                    return false; // 중단
                }
                return true; // 계속
            });
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
    /// <summary>
    /// 인벤토리 빈슬롯 생성
    /// </summary>
    public void CreateEmptySlots()
    {
        //for (int i = 0; i < inventoryData.SlotCount; i++)
        //{
        //    GameObject emptySlot = new GameObject($"EmptySlot_{i}");
        //    emptySlot.transform.SetParent(contentContainer);
        //    emptySlotList.Add(emptySlot.transform);
        //}

        if (contentContainer == null)
        {
            Debug.LogError("[GameInventoryUI] contentContainer가 할당되지 않았습니다!");
            return;
        }

        // 현재 필요한 슬롯 개수 (Tier에 따라 10/20/30)
        int requiredSlots = inventoryData.SlotCount;
        int currentSlots = emptySlotList.Count;

        Debug.Log($"[GameInventoryUI] 슬롯 체크 - 현재: {currentSlots}개, 필요: {requiredSlots}개 (Tier {inventoryData.InventoryTier})");

        // 슬롯 추가
        if (requiredSlots > currentSlots)
        {
            int slotsToAdd = requiredSlots - currentSlots;

            for (int i = 0; i < slotsToAdd; i++)
            {
                int slotIndex = currentSlots + i;
                var emptySlot = Instantiate(emptySlotPrefab);
                emptySlot.transform.SetParent(contentContainer, false); // worldPositionStays = false
                emptySlot.transform.localScale = Vector3.one;

                emptySlotList.Add(emptySlot.transform);
            }

            Debug.Log($"[GameInventoryUI] 빈 슬롯 {slotsToAdd}개 추가 (총 {emptySlotList.Count}개)");
        }
        // 슬롯 제거 (Tier가 내려가는 경우 - 보통 없음)
        else if (requiredSlots < currentSlots)
        {
            int slotsToRemove = currentSlots - requiredSlots;

            for (int i = 0; i < slotsToRemove; i++)
            {
                int lastIndex = emptySlotList.Count - 1;
                if (lastIndex >= 0)
                {
                    Transform slotToRemove = emptySlotList[lastIndex];
                    emptySlotList.RemoveAt(lastIndex);

                    if (slotToRemove != null)
                    {
                        Destroy(slotToRemove.gameObject);
                    }
                }
            }

            Debug.Log($"[GameInventoryUI] 빈 슬롯 {slotsToRemove}개 제거 (총 {emptySlotList.Count}개)");
        }
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