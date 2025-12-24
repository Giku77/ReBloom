using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuickSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 6;

    [Header("UI References")]
    [SerializeField] private List<GameObject> slotsRef;
    [SerializeField] private List<GameObject> invtQuickslotRef;
    [SerializeField] private QuickSlotUI quickSlotUIPrefab;

    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    [SerializeField] private GameInventory gameInventory;

    [Header("Slot Action")]
    [SerializeField] private InputAction quickAction1;
    [SerializeField] private InputAction quickAction2;
    [SerializeField] private InputAction quickAction3;
    [SerializeField] private InputAction quickAction4;
    [SerializeField] private InputAction quickAction5;
    [SerializeField] private InputAction quickAction6;

    private ItemBase[] items;
    private QuickSlotUI[] slotUIs;
    private QuickSlotUI[] invSlotUIs;

    private int assignedSlotCount = 0;

    public ReadOnlyCollection<ItemBase> GetItemBaseSlot => Array.AsReadOnly(items);
    public int AssignedSlotCount => assignedSlotCount;
    public int MaxSlotCount => slotCount;

    public event Action<ItemBase, int> OnSlotAssign;
    public event Action<int> OnSlotRemoved;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSlots();
    }

    private void Start()
    {
        ValidateReferences();
        SubscribeToInventoryEvents();
    }

    private void OnEnable()
    {
        quickAction1.Enable();
        quickAction1.started += OnQuickSlot1;
        quickAction2.Enable();
        quickAction2.started += OnQuickSlot2;
        quickAction3.Enable();
        quickAction3.started += OnQuickSlot3;
        quickAction4.Enable();
        quickAction4.started += OnQuickSlot4;
        quickAction5.Enable();
        quickAction5.started += OnQuickSlot5;
        quickAction6.Enable();
        quickAction6.started += OnQuickSlot6;
    }

    private void OnDisable()
    {
        quickAction1.started -= OnQuickSlot1;
        quickAction1.Disable();
        quickAction2.started -= OnQuickSlot2;
        quickAction2.Disable();
        quickAction3.started -= OnQuickSlot3;
        quickAction3.Disable();
        quickAction4.started -= OnQuickSlot4;
        quickAction4.Disable();
        quickAction5.started -= OnQuickSlot5;
        quickAction5.Disable();
        quickAction6.started -= OnQuickSlot6;
        quickAction6.Disable();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInventoryEvents();
    }
    #endregion

    #region Initialization
    private void InitializeSlots()
    {
        items = new ItemBase[slotCount];
        slotUIs = new QuickSlotUI[slotCount];
        invSlotUIs = new QuickSlotUI[slotCount];
        assignedSlotCount = 0;
        Debug.Log($"[QuickSlot] {slotCount}개 슬롯 초기화 완료");
    }

    private void ValidateReferences()
    {
        if (slotsRef == null || slotsRef.Count == 0)
        {
            Debug.LogError("[QuickSlot] slotsRef가 비어있습니다!", this);
            return;
        }

        if (slotsRef.Count < slotCount)
        {
            Debug.LogWarning($"[QuickSlot] slotsRef 개수({slotsRef.Count})가 slotCount({slotCount})보다 적습니다!", this);
        }

        if (quickSlotUIPrefab == null)
        {
            Debug.LogError("[QuickSlot] QuickSlotUI 프리팹이 할당되지 않았습니다!", this);
        }

        if (inventoryData == null)
        {
            Debug.LogWarning("[QuickSlot] InventoryData가 할당되지 않았습니다!", this);
        }

        if (gameInventory == null)
        {
            Debug.LogWarning("[QuickSlot] GameInventory가 할당되지 않았습니다!", this);
        }
    }
    #endregion

    #region Event Subscription
    /// <summary>
    /// 인벤토리 변경 이벤트 구독
    /// </summary>
    private void SubscribeToInventoryEvents()
    {
        if (inventoryData != null)
        {
            inventoryData.OnContainerChanged += OnInventoryChanged;
            Debug.Log("[QuickSlot] 인벤토리 변경 이벤트 구독 완료");
        }
    }

    private void UnsubscribeFromInventoryEvents()
    {
        if (inventoryData != null)
        {
            inventoryData.OnContainerChanged -= OnInventoryChanged;
        }
    }

    /// <summary>
    /// 인벤토리 변경 시 자동 동기화
    /// </summary>
    private void OnInventoryChanged()
    {
        UpdateAllSlotQuantities();
    }
    #endregion

    #region Slot Assignment
    /// <summary>
    /// 아이템을 퀵슬롯에 배치 시도
    /// </summary>
    public bool TryAssign(ItemBase item, int quantity)
    {
        if (item == null)
        {
            Debug.LogWarning("[QuickSlot] null 아이템은 배치할 수 없습니다.");
            return false;
        }

        // 유효성 검증 추가
        if (!CanAssignToQuickSlot(item))
        {
            return false;
        }

        if (assignedSlotCount >= slotCount)
        {
            Debug.LogWarning("[QuickSlot] 슬롯이 가득 찼습니다.");
            return false;
        }

        if (IsItemAlreadyAssigned(item))
        {
            Debug.LogWarning($"[QuickSlot] {item.itemName}은(는) 이미 배치되어 있습니다.");
            return false;
        }

        Assign(item, quantity);
        return true;
    }

    /// <summary>
    /// 특정 슬롯에 직접 할당 (드래그 앤 드롭용)
    /// </summary>
    public bool AssignToSlot(int targetIndex, ItemBase item, int quantity)
    {
        if (targetIndex < 0 || targetIndex >= slotCount)
        {
            Debug.LogError($"[QuickSlot] 잘못된 슬롯 인덱스: {targetIndex}");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("[QuickSlot] null 아이템은 배치할 수 없습니다.");
            return false;
        }

        // 유효성 검증
        if (!CanAssignToQuickSlot(item))
        {
            return false;
        }

        // 기존 아이템이 있으면 제거
        if (items[targetIndex] != null)
        {
            if (slotUIs[targetIndex] != null)
            {
                Destroy(slotUIs[targetIndex].gameObject);
                slotUIs[targetIndex] = null;
            }
        }
        else
        {
            assignedSlotCount++;
        }

        // 새 아이템 할당
        items[targetIndex] = item;
        CreateSlotUI(targetIndex, item, quantity);
        OnSlotAssign?.Invoke(item, quantity);

        Debug.Log($"[QuickSlot] {item.itemName} x{quantity}를 슬롯 {targetIndex}에 할당");
        return true;
    }

    private void Assign(ItemBase item, int quantity)
    {
        int targetIndex = FindNextEmptySlot();

        if (targetIndex == -1)
        {
            Debug.LogError("[QuickSlot] 빈 슬롯을 찾을 수 없습니다!");
            return;
        }

        if (targetIndex < 0 || targetIndex >= slotsRef.Count)
        {
            Debug.LogError($"[QuickSlot] 슬롯 인덱스 {targetIndex}가 범위를 벗어났습니다.");
            return;
        }

        items[targetIndex] = item;
        assignedSlotCount++;
        CreateSlotUI(targetIndex, item, quantity);
        OnSlotAssign?.Invoke(item, quantity);

        Debug.Log($"[QuickSlot] {item.itemName} x{quantity}를 슬롯 {targetIndex}에 배치");
    }

    private void CreateSlotUI(int index, ItemBase item, int quantity)
    {
        if (quickSlotUIPrefab == null)
        {
            Debug.LogError("[QuickSlot] QuickSlotUI 프리팹이 없습니다!");
            return;
        }

        // 게임 내 퀵슬롯 UI 생성
        if (slotsRef != null && index < slotsRef.Count && slotsRef[index] != null)
        {
            if (slotUIs[index] != null)
            {
                Destroy(slotUIs[index].gameObject);
            }

            QuickSlotUI newSlotUI = Instantiate(
                quickSlotUIPrefab,
                slotsRef[index].transform.position,
                Quaternion.identity,
                slotsRef[index].transform
            );

            newSlotUI.OnUpdateSlotInfo(item, quantity);
            SetDragDropHandlerData(item, newSlotUI);
            slotUIs[index] = newSlotUI;
        }

        // 인벤토리 내 퀵슬롯 UI 생성 (동기화)
        if (invtQuickslotRef != null && index < invtQuickslotRef.Count && invtQuickslotRef[index] != null)
        {
            if (invSlotUIs[index] != null)
            {
                Destroy(invSlotUIs[index].gameObject);
            }

            QuickSlotUI invSlotUI = Instantiate(
                quickSlotUIPrefab,
                invtQuickslotRef[index].transform.position,
                Quaternion.identity,
                invtQuickslotRef[index].transform
            );

            invSlotUI.OnUpdateSlotInfo(item, quantity);
            SetDragDropHandlerData(item, invSlotUI);
            invSlotUIs[index] = invSlotUI;
        }
    }

    #region Validation
    /// <summary>
    /// 퀵슬롯에 배치 가능한지 검증
    /// </summary>
    private bool CanAssignToQuickSlot(ItemBase item)
    {
        if (item == null)
        {
            return false;
        }

        // 1. 아이템의 canQuickSlot 플래그 확인
        if (!item.canQuickSlot)
        {
            Debug.LogWarning($"[QuickSlot] {item.itemName}은(는) 퀵슬롯에 배치할 수 없습니다.");
            return false;
        }

        // 2. 인벤토리에 아이템이 있는지 확인
        if (gameInventory != null && !gameInventory.HasItem(item.itemID, 1))
        {
            Debug.LogWarning($"[QuickSlot] 인벤토리에 {item.itemName}이(가) 없습니다.");
            return false;
        }

        return true;
    }
    #endregion

    #region Slot Removal
    public bool RemoveSlot(int index)
    {
        if (index < 0 || index >= slotCount)
        {
            Debug.LogError($"[QuickSlot] 잘못된 슬롯 인덱스: {index}");
            return false;
        }

        if (items[index] == null)
        {
            Debug.LogWarning($"[QuickSlot] 슬롯 {index}는 이미 비어있습니다.");
            return false;
        }

        items[index] = null;

        // 게임 내 퀵슬롯 UI 제거
        if (slotUIs[index] != null)
        {
            Destroy(slotUIs[index].gameObject);
            slotUIs[index] = null;
        }

        // 인벤토리 내 퀵슬롯 UI 제거
        if (invSlotUIs[index] != null)
        {
            Destroy(invSlotUIs[index].gameObject);
            invSlotUIs[index] = null;
        }

        assignedSlotCount--;
        OnSlotRemoved?.Invoke(index);

        Debug.Log($"[QuickSlot] 슬롯 {index} 제거됨");
        return true;
    }
    #endregion

    public bool RemoveItem(ItemBase item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemID == item.itemID)
            {
                return RemoveSlot(i);
            }
        }

        Debug.LogWarning($"[QuickSlot] {item.itemName}을(를) 찾을 수 없습니다.");
        return false;
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null)
            {
                RemoveSlot(i);
            }
        }
        Debug.Log("[QuickSlot] 모든 슬롯 초기화 완료");
    }
    #endregion

    #region Slot Queries
    private int FindNextEmptySlot()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    public bool IsItemAlreadyAssigned(ItemBase item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemID == item.itemID)
            {
                return true;
            }
        }
        return false;
    }

    public ItemBase GetItemAtSlot(int index)
    {
        if (index < 0 || index >= slotCount)
        {
            Debug.LogError($"[QuickSlot] 잘못된 슬롯 인덱스: {index}");
            return null;
        }

        return items[index];
    }

    public int FindItemSlot(ItemBase item)
    {
        if (item == null) return -1;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemID == item.itemID)
            {
                return i;
            }
        }
        return -1;
    }
    #endregion

    #region Slot Updates
    public void UpdateSlotQuantity(int index)
    {
        if (index < 0 || index >= slotCount) return;
        if (items[index] == null) return;
        if (inventoryData == null) return;

        int currentQuantity = inventoryData.GetItemCount(items[index].itemID);

        // 게임 내 퀵슬롯 UI 업데이트
        if (slotUIs[index] != null)
        {
            slotUIs[index].OnUpdateSlotInfo(items[index], currentQuantity);
        }

        // 인벤토리 내 퀵슬롯 UI 업데이트
        if (invSlotUIs[index] != null)
        {
            invSlotUIs[index].OnUpdateSlotInfo(items[index], currentQuantity);
        }

        if (currentQuantity <= 0)
        {
            RemoveSlot(index);
        }
    }

    public void UpdateAllSlotQuantities()
    {
        for (int i = 0; i < slotCount; i++)
        {
            UpdateSlotQuantity(i);
        }
    }
    #endregion

    #region 초기 동기화
    /// <summary>
    /// 인벤토리 창이 열릴 때 호출하여 기존 퀵슬롯 상태를 동기화
    /// </summary>
    public void SyncInventoryQuickSlots()
    {
        if (invtQuickslotRef == null || invtQuickslotRef.Count == 0)
        {
            Debug.LogWarning("[QuickSlot] invtQuickslotRef가 설정되지 않음");
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null && i < invtQuickslotRef.Count)
            {
                // 이미 UI가 있으면 업데이트만, 없으면 생성
                if (invSlotUIs[i] == null)
                {
                    int quantity = inventoryData.GetItemCount(items[i].itemID);

                    QuickSlotUI invSlotUI = Instantiate(
                        quickSlotUIPrefab,
                        invtQuickslotRef[i].transform.position,
                        Quaternion.identity,
                        invtQuickslotRef[i].transform
                    );

                    invSlotUI.OnUpdateSlotInfo(items[i], quantity);
                    SetDragDropHandlerData(items[i], invSlotUI);
                    invSlotUIs[i] = invSlotUI;
                }
            }
        }

        Debug.Log("[QuickSlot] 인벤토리 퀵슬롯 동기화 완료");
    }
    #endregion

    #region Auto Fill(디버그 용도)
    /// <summary>
    /// 퀵슬롯 자동 배치 (O키 기능)
    /// </summary>
    [ContextMenu("Auto Fill Quick Slots")]
    public int AutoFillQuickSlots()
    {
        if (inventoryData == null || gameInventory == null)
        {
            Debug.LogWarning("[QuickSlot] InventoryData 또는 GameInventory가 없습니다!");
            return 0;
        }

        int filledCount = 0;

        // ItemSlotData로 순회
        foreach (var slot in inventoryData.Items)
        {
            int itemId = slot.itemID;
            int quantity = slot.count;

            ItemBase item = ItemDatabase.I.GetItem(itemId);

            if (item != null && CanAssignToQuickSlot(item))
            {
                if (IsItemAlreadyAssigned(item))
                {
                    continue;
                }

                if (TryAssign(item, quantity))
                {
                    filledCount++;
                }
                else
                {
                    break; // 슬롯 가득 참
                }
            }
        }

        Debug.Log($"[QuickSlot] 자동 배치 완료: {filledCount}개 아이템");
        return filledCount;
    }
    #endregion

    public void SetDragDropHandlerData(ItemBase item, QuickSlotUI slot)
    {
        if (!slot.TryGetComponent(out ItemIconDragHandler dragHandler))
        {
            Debug.LogError("[QuickSlot] ItemIconDragHandler 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        dragHandler.SetItemData(item);
    }

    #region QuickSlot 사용
    public void OnQuickSlot1(InputAction.CallbackContext context)
    {
        UseSlot(0);
    }
    public void OnQuickSlot2(InputAction.CallbackContext context)
    {
        UseSlot(1);
    }
    public void OnQuickSlot3(InputAction.CallbackContext context)
    {
        UseSlot(2);
    }
    public void OnQuickSlot4(InputAction.CallbackContext context)
    {
        UseSlot(3);
    }
    public void OnQuickSlot5(InputAction.CallbackContext context)
    {
        UseSlot(4);
    }
    public void OnQuickSlot6(InputAction.CallbackContext context)
    {
        UseSlot(5);
    }



    void UseSlot(int index)
    {
        var item = items[index];

        if (item != null)
        {
            gameInventory.Consume(item.itemID, 1);
        }
        else
        {
            Debug.LogError($"[QuickSlot] 퀵슬롯 {index + 1}에는 아이템이 없습니다.");
        }
    }
    #endregion

}