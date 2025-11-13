using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// 퀵슬롯 관리 시스템
/// 아이템을 빠르게 사용할 수 있도록 슬롯에 배치
/// </summary>
public class QuickSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 7;

    [Header("UI References")]
    [SerializeField] private List<GameObject> slotsRef;  // 슬롯 위치 참조
    [SerializeField] private QuickSlotUI quickSlotUIPrefab;  // UI 프리팹

    [Header("Data Reference")]
    [SerializeField] private InventoryItemData inventoryData;  // 인벤토리 데이터

    // 내부 데이터
    private ItemBase[] items;  // 슬롯에 할당된 아이템들
    private QuickSlotUI[] slotUIs;  // 생성된 UI 인스턴스들
    private int assignedSlotCount = 0;

    // 외부 접근
    public ReadOnlyCollection<ItemBase> GetItemBaseSlot => Array.AsReadOnly(items);
    public int AssignedSlotCount => assignedSlotCount;
    public int MaxSlotCount => slotCount;

    // 이벤트
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
    }
    #endregion

    #region Initialization
    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    private void InitializeSlots()
    {
        items = new ItemBase[slotCount];
        slotUIs = new QuickSlotUI[slotCount];
        assignedSlotCount = 0;

        Debug.Log($"[QuickSlot] {slotCount}개 슬롯 초기화 완료");
    }

    /// <summary>
    /// 참조 검증
    /// </summary>
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
            Debug.LogWarning("[QuickSlot] InventoryData가 할당되지 않았습니다. 수량 동기화가 작동하지 않습니다.", this);
        }
    }
    #endregion

    #region Slot Assignment
    /// <summary>
    /// 아이템을 퀵슬롯에 배치 시도
    /// </summary>
    public bool TryAssign(ItemBase item, int quantity)
    {
        // 유효성 검사
        if (item == null)
        {
            Debug.LogWarning("[QuickSlot] null 아이템은 배치할 수 없습니다.");
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

        // 배치
        Assign(item, quantity);
        return true;
    }

    /// <summary>
    /// 아이템을 슬롯에 배치
    /// </summary>
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

        Debug.Log($"[QuickSlot] {item.itemName} x{quantity}를 슬롯 {targetIndex}에 배치 (총 {assignedSlotCount}/{slotCount})");
    }

    /// <summary>
    /// 슬롯 UI 생성
    /// </summary>
    private void CreateSlotUI(int index, ItemBase item, int quantity)
    {
        if (quickSlotUIPrefab == null)
        {
            Debug.LogError("[QuickSlot] QuickSlotUI 프리팹이 없습니다!");
            return;
        }

        // 기존 UI가 있으면 제거
        if (slotUIs[index] != null)
        {
            Destroy(slotUIs[index].gameObject);
        }

        // 새 UI 생성
        QuickSlotUI newSlotUI = Instantiate(
            quickSlotUIPrefab,
            slotsRef[index].transform.position,
            Quaternion.identity,
            slotsRef[index].transform  // 부모 설정
        );

        // UI 업데이트
        newSlotUI.OnUpdateSlotInfo(item, quantity);

        // 참조 저장
        slotUIs[index] = newSlotUI;
    }
    #endregion

    #region Slot Removal
    /// <summary>
    /// 특정 슬롯의 아이템 제거
    /// </summary>
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

        // 아이템 제거
        items[index] = null;

        // UI 제거
        if (slotUIs[index] != null)
        {
            Destroy(slotUIs[index].gameObject);
            slotUIs[index] = null;
        }

        assignedSlotCount--;

        // 이벤트 발생
        OnSlotRemoved?.Invoke(index);

        Debug.Log($"[QuickSlot] 슬롯 {index} 제거됨");
        return true;
    }

    /// <summary>
    /// 특정 아이템을 찾아서 제거 (ItemID 기반)
    /// </summary>
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

    /// <summary>
    /// 모든 슬롯 초기화
    /// </summary>
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
    /// <summary>
    /// 다음 빈 슬롯 인덱스 찾기
    /// </summary>
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

    /// <summary>
    /// 아이템이 이미 배치되어 있는지 확인
    /// </summary>
    public bool IsItemAlreadyAssigned(ItemBase item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemID == item.itemID)
            {
                Debug.LogWarning($"[QuickSlot] {item.itemName}(ID:{item.itemID})은 이미 슬롯 {i}에 배치되어 있습니다.");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 슬롯의 아이템 가져오기
    /// </summary>
    public ItemBase GetItemAtSlot(int index)
    {
        if (index < 0 || index >= slotCount)
        {
            Debug.LogError($"[QuickSlot] 잘못된 슬롯 인덱스: {index}");
            return null;
        }

        return items[index];
    }

    /// <summary>
    /// 아이템이 있는 슬롯 인덱스 찾기 (ItemID 기반)
    /// </summary>
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
    /// <summary>
    /// 슬롯의 수량 업데이트 (인벤토리와 동기화)
    /// </summary>
    public void UpdateSlotQuantity(int index)
    {
        if (index < 0 || index >= slotCount) return;
        if (items[index] == null) return;
        if (slotUIs[index] == null) return;
        if (inventoryData == null) return;

        // 인벤토리에서 현재 수량 가져오기
        int currentQuantity = inventoryData.GetItemCount(items[index].itemID);

        // UI 업데이트
        slotUIs[index].OnUpdateSlotInfo(items[index], currentQuantity);

        // 수량이 0이면 슬롯에서 제거
        if (currentQuantity <= 0)
        {
            RemoveSlot(index);
        }
    }

    /// <summary>
    /// 모든 슬롯의 수량 업데이트
    /// </summary>
    public void UpdateAllSlotQuantities()
    {
        for (int i = 0; i < slotCount; i++)
        {
            UpdateSlotQuantity(i);
        }
    }
    #endregion

    #region Debug
    [ContextMenu("Debug/Print Slot Status")]
    public void CMD_PrintSlotStatus()
    {
        Debug.Log("=== QuickSlot 상태 ===");
        Debug.Log($"할당된 슬롯: {assignedSlotCount}/{slotCount}");

        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null)
            {
                int quantity = inventoryData != null ? inventoryData.GetItemCount(items[i].itemID) : 0;
                Debug.Log($"슬롯 [{i}]: {items[i].name} x{quantity}");
            }
            else
            {
                Debug.Log($"슬롯 [{i}]: 비어있음");
            }
        }
    }

    [ContextMenu("Debug/Clear All Slots")]
    public void CMD_ClearAllSlots()
    {
        ClearAllSlots();
    }
    #endregion
}