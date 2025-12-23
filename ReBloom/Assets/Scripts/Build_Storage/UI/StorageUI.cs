using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리지 UI - View만 담당 (표시만)
/// 비즈니스 로직은 IItemContainer.TransferTo 활용
/// </summary>
public class StorageUI : UIBase
{
    [Header("Data References")]
    [SerializeField] private GameInventory inventoryData; // 인벤토리 참조
    private StorageData storageData; // 런타임 인스턴스
    private WorldStorage worldStorage; // 상호작용 대상

    [Header("UI Settings")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private GameObject storageSlotPrefab;

    [Header("UI Root")]
    [SerializeField] private GameObject storageUIRoot;

    [Header("Inventory Panel (창고 열 때 같이 표시)")]
    [SerializeField] private ContainerSlotsUI inventoryPanel;

    private readonly List<Transform> emptySlots = new List<Transform>();
    private readonly List<StorageSlot> activeSlots = new List<StorageSlot>();

    #region Unity 생명주기
    private void Start()
    {
        //if (storageUIRoot != null)
        //{
        //    storageUIRoot.SetActive(false);
        //}
    }

    private void OnDestroy()
    {
        if (storageData != null)
        {
            storageData.OnStorageChanged -= RefreshUI;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// WorldStorage에서 호출되는 초기화 메서드
    /// </summary>
    public void Initialize(StorageData data, WorldStorage storage)
    {
        // 기존 이벤트 구독 해제
        if (storageData != null)
        {
            storageData.OnStorageChanged -= RefreshUI;
        }

        // 새 데이터 설정
        storageData = data;
        worldStorage = storage;

        if (storageData == null)
        {
            Debug.LogError("[StorageUI] StorageData가 null입니다!");
            return;
        }

        if (inventoryData == null)
        {
            Debug.LogError("[StorageUI] InventoryItemData가 할당되지 않았습니다!");
            return;
        }

        // 이벤트 구독
        storageData.OnStorageChanged += RefreshUI;

        // EmptySlot 재생성 (티어가 다를 수 있음)
        CreateEmptySlots();

        Debug.Log($"[StorageUI] 초기화 완료 - Storage: {storage.name}, Tier: {storageData.StorageTier}, 슬롯: {storageData.SlotCount}개");
    }

    /// <summary>
    /// EmptySlot 동적 생성 (DropZoneMarker 포함)
    /// </summary>
    private void CreateEmptySlots()
    {
        if (slotContainer == null)
        {
            Debug.LogError("[StorageUI] slotContainer가 할당되지 않았습니다!");
            return;
        }

        if (emptySlotPrefab == null)
        {
            Debug.LogError("[StorageUI] emptySlotPrefab이 할당되지 않았습니다!");
            return;
        }

        // 기존 EmptySlot 정리
        ClearEmptySlots();

        int slotCount = storageData.SlotCount;

        // EmptySlot 동적 생성
        for (int i = 0; i < slotCount; i++)
        {
            GameObject emptySlotObj = Instantiate(emptySlotPrefab, slotContainer);
            emptySlotObj.transform.localScale = Vector3.one;
            emptySlotObj.name = $"EmptySlot_{i:D2}";

            // DropZoneMarker 초기화
            var dropZone = emptySlotObj.GetComponent<DropZoneMarker>();
            if (dropZone != null)
            {
                dropZone.SetZoneType(DropZoneType.Storage);
                dropZone.SetSlotIndex(i);
                dropZone.SetPriority(80);
            }
            else
            {
                Debug.LogWarning($"[StorageUI] EmptySlot_{i}에 DropZoneMarker가 없습니다!");
            }

            emptySlotObj.SetActive(true);
            emptySlots.Add(emptySlotObj.transform);
        }

        Debug.Log($"[StorageUI] {emptySlots.Count}개 EmptySlot 생성 완료");
    }

    private void ClearEmptySlots()
    {
        foreach (var emptySlot in emptySlots)
        {
            if (emptySlot != null)
            {
                Destroy(emptySlot.gameObject);
            }
        }
        emptySlots.Clear();
    }
    #endregion

    #region UI 갱신 (View 역할)
    /// <summary>
    /// 스토리지 UI 갱신 - 표시만 담당
    /// </summary>
    public void RefreshUI()
    {
        if (storageData == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[StorageUI] StorageData 또는 ItemDatabase가 없습니다.");
            return;
        }

        // 기존 StorageSlot 정리
        ClearActiveSlots();

        // 스토리지 데이터 가져오기
        var items = storageData.GetAllItems();

        // 아이템 슬롯 생성
        int slotIndex = 0;
        foreach (var itemSlot in items)
        {
            if (slotIndex >= emptySlots.Count) break;

            ItemBase item = ItemDatabase.I.GetItem(itemSlot.itemID);
            if (item != null && itemSlot.count > 0)
            {
                CreateItemSlot(item, itemSlot.count, slotIndex);
                slotIndex++;
            }
        }

        Debug.Log($"[StorageUI] UI 갱신 완료 - {slotIndex}개 아이템 표시");
    }
    #endregion

    /// <summary>
    /// 활성화된 StorageSlot 정리
    /// </summary>
    private void ClearActiveSlots()
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

    /// <summary>
    /// 아이템 슬롯 생성 (EmptySlot의 자식으로)
    /// </summary>
    private void CreateItemSlot(ItemBase item, int quantity, int slotIndex)
    {
        if (storageSlotPrefab == null || emptySlots == null || slotIndex >= emptySlots.Count)
        {
            Debug.LogError("[StorageUI] storageSlotPrefab 또는 emptySlots가 없거나 슬롯 인덱스 초과!");
            return;
        }

        // EmptySlot의 자식으로 StorageSlot 생성
        GameObject slotObj = Instantiate(storageSlotPrefab, emptySlots[slotIndex]);
        slotObj.transform.localScale = Vector3.one;
        slotObj.name = $"StorageSlot_{slotIndex}";
       //Debug.Log($"[StorageUI] StorageSlot 생성: {slotObj.name}, 부모: {emptySlots[slotIndex].name}");

        if (!slotObj.TryGetComponent(out StorageSlot slot))
        {
            Debug.LogError("[StorageUI] StorageSlot 컴포넌트를 찾을 수 없습니다!");
            Destroy(slotObj);
            return;
        }

        // IDragSource 구현 확인
        if (slot is IDragSource dragSource)
        {
           // Debug.Log($"[StorageUI] StorageSlot은 IDragSource를 구현하고 있습니다. SourceType: {dragSource.SourceType}");
        }
        else
        {
            Debug.LogError("[StorageUI] StorageSlot이 IDragSource를 구현하지 않았습니다!");
        }

        // ItemIconDragHandler 확인
        var dragHandler = slotObj.GetComponentInChildren<ItemIconDragHandler>();
        if (dragHandler != null)
        {
           // Debug.Log($"[StorageUI] ItemIconDragHandler 찾음: {dragHandler.name}");
            dragHandler.SetItemData(item);
        }
        else
        {
            Debug.LogError($"[StorageUI] StorageSlot에 ItemIconDragHandler가 없습니다!");
        }

        // IItemSlot 인터페이스 메서드 사용
        slot.SetItem(item, quantity);

        // 활성 슬롯 리스트에 추가
        activeSlots.Add(slot);
        slotObj.SetActive(true);
    }

    #region 아이템 관리 (TransferTo 활용)

    /// <summary>
    /// 스토리지에서 아이템 회수 (인벤토리로)
    /// </summary>
    public void WithdrawItem(int slotIndex)
    {
        if (storageData == null || inventoryData == null)
        {
            Debug.LogError("[StorageUI] StorageData 또는 InventoryData가 없습니다!");
            return;
        }

        if (slotIndex < 0 || slotIndex >= activeSlots.Count)
        {
            Debug.LogError($"[StorageUI] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return;
        }

        StorageSlot slot = activeSlots[slotIndex];
        ItemBase item = slot.GetItem();

        if (item == null)
        {
            Debug.LogWarning("[StorageUI] 빈 슬롯입니다.");
            return;
        }

        // TransferTo 사용 (Storage -> Inventory) - 1개만
        bool success = storageData.TransferTo(inventoryData.Container, item.itemID, 1);

        if (success)
        {
            Debug.Log($"[StorageUI] 아이템 회수 성공: {item.itemName}");
        }
        else
        {
            Debug.LogWarning($"[StorageUI] 아이템 회수 실패: {item.itemName} (인벤토리 가득참?)");
        }
    }
    #endregion
    #region 전체 이동 버튼

    /// <summary>
    /// 창고 → 인벤토리 (전부 가져오기)
    /// </summary>
    public void OnClickWithdrawAll()
    {
        if (storageData == null || inventoryData == null)
            return;

        bool success = inventoryData.WithdrawAllFrom(storageData);

        if (success)
            Debug.Log("[StorageUI] 전체 회수 완료!");
        else
            Debug.LogWarning("[StorageUI] 일부만 회수됨");
    }

    public void OnClickDepositAll()
    {
        if (storageData == null || inventoryData == null)
            return;

        bool success = inventoryData.DepositAllTo(storageData);

        if (success)
            Debug.Log("[StorageUI] 전체 보관 완료!");
        else
            Debug.LogWarning("[StorageUI] 일부만 보관됨");
    }

    #endregion
    #region UI 토글
    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;
        Debug.Log($"[StorageUI] Toggle 호출됨 - Type: {Type}");
        UIManager.Instance?.ToggleUI(Type);
    }

    // UIBase의 OnShow/OnHide 오버라이드로 로직 이동
    protected override void OnShow()
    {
        base.OnShow();
        RefreshUI();

        // 인벤토리 패널 바인딩
        if (inventoryPanel != null && inventoryData != null && inventoryData.Container is InventoryItemData)
        {
            inventoryPanel.Bind((InventoryItemData)inventoryData.Container);
        }

        SoundManager.I?.PlayOpenBox();
        DragDropManager.I?.SetCurrentStorage(worldStorage);
        Debug.Log("[StorageUI] 창고 UI 열림");
    }

    protected override void OnHide()
    {
        base.OnHide();
        // 인벤토리 패널 언바인딩
        inventoryPanel?.Unbind();

        DragDropManager.I?.SetCurrentStorage(null);

        SoundManager.I?.PlayCloseCraftingTable();
        var player = FindFirstObjectByType<PlayerController>();
        player?.SetCurrentStorage(null);

        Debug.Log("[StorageUI] 창고 UI 닫힘");
    }
    #endregion
}