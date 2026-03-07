using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리지 UI - View만 담당
/// </summary>
public class StorageUI : UIBase
{
    [Header("Data References")]
    [SerializeField] private GameInventory inventoryData;
    private StorageData storageData;
    private WorldStorage worldStorage;

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

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
    }

    private void OnDestroy()
    {
        if (storageData != null)
            storageData.OnStorageChanged -= RefreshUI;
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null)
            return;

        var inventory = playerObj.GetComponent<GameInventory>();
        if (inventory != null)
            inventoryData = inventory;
    }

    public void Initialize(StorageData data, WorldStorage storage)
    {
        if (storageData != null)
            storageData.OnStorageChanged -= RefreshUI;

        storageData = data;
        worldStorage = storage;

        if (storageData == null)
        {
            Debug.LogError("[StorageUI] StorageData가 null입니다!");
            return;
        }

        if (inventoryData == null)
            inventoryData = FindFirstObjectByType<GameInventory>();

        if (inventoryData == null)
        {
            Debug.LogError("[StorageUI] GameInventory가 할당되지 않았습니다!");
            return;
        }

        storageData.OnStorageChanged += RefreshUI;
        CreateEmptySlots();

        Debug.Log($"[StorageUI] 초기화 완료 - Storage: {storage.name}, Tier: {storageData.StorageTier}, 슬롯: {storageData.SlotCount}개");
    }

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

        ClearEmptySlots();

        int slotCount = storageData.SlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            GameObject emptySlotObj = Instantiate(emptySlotPrefab, slotContainer);
            emptySlotObj.transform.localScale = Vector3.one;
            emptySlotObj.name = $"EmptySlot_{i:D2}";

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
    }

    private void ClearEmptySlots()
    {
        foreach (var emptySlot in emptySlots)
        {
            if (emptySlot != null)
                Destroy(emptySlot.gameObject);
        }

        emptySlots.Clear();
    }

    public void RefreshUI()
    {
        if (storageData == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[StorageUI] StorageData 또는 ItemDatabase가 없습니다.");
            return;
        }

        ClearActiveSlots();

        var items = storageData.GetAllItems();
        int slotIndex = 0;
        foreach (var itemSlot in items)
        {
            if (slotIndex >= emptySlots.Count)
                break;

            ItemBase item = ItemDatabase.I.GetItem(itemSlot.itemID);
            if (item != null && itemSlot.count > 0)
            {
                CreateItemSlot(item, itemSlot.count, slotIndex);
                slotIndex++;
            }
        }
    }

    private void ClearActiveSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        activeSlots.Clear();
    }

    private void CreateItemSlot(ItemBase item, int quantity, int slotIndex)
    {
        if (storageSlotPrefab == null || slotIndex >= emptySlots.Count)
        {
            Debug.LogError("[StorageUI] storageSlotPrefab 또는 emptySlots가 없거나 슬롯 인덱스 초과!");
            return;
        }

        GameObject slotObj = Instantiate(storageSlotPrefab, emptySlots[slotIndex]);
        slotObj.transform.localScale = Vector3.one;
        slotObj.name = $"StorageSlot_{slotIndex}";

        if (!slotObj.TryGetComponent(out StorageSlot slot))
        {
            Debug.LogError("[StorageUI] StorageSlot 컴포넌트를 찾을 수 없습니다!");
            Destroy(slotObj);
            return;
        }

        if (!(slot is IDragSource))
        {
            Debug.LogError("[StorageUI] StorageSlot이 IDragSource를 구현하지 않았습니다!");
        }

        var dragHandler = slotObj.GetComponentInChildren<ItemIconDragHandler>();
        if (dragHandler != null)
            dragHandler.SetItemData(item);
        else
            Debug.LogError("[StorageUI] StorageSlot에 ItemIconDragHandler가 없습니다!");

        slot.SetItem(item, quantity);
        activeSlots.Add(slot);
        slotObj.SetActive(true);
    }

    public void WithdrawItem(int slotIndex)
    {
        if (storageData == null || inventoryData == null || worldStorage == null)
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

        bool success = worldStorage.RequestWithdrawToInventory(item.itemID, 1);
        if (success)
            Debug.Log($"[StorageUI] 아이템 회수 성공: {item.itemName}");
        else
            Debug.LogWarning($"[StorageUI] 아이템 회수 실패: {item.itemName} (인벤토리 가득참?)");
    }

    public void OnClickWithdrawAll()
    {
        if (storageData == null || inventoryData == null || worldStorage == null)
            return;

        bool success = worldStorage.RequestWithdrawAllToInventory();
        if (success)
            Debug.Log("[StorageUI] 전체 회수 완료!");
        else
            Debug.LogWarning("[StorageUI] 일부만 회수되었거나 이동할 아이템이 없습니다.");
    }

    public void OnClickDepositAll()
    {
        if (storageData == null || inventoryData == null || worldStorage == null)
            return;

        bool success = worldStorage.RequestDepositAllFromInventory();
        if (success)
            Debug.Log("[StorageUI] 전체 보관 완료!");
        else
            Debug.LogWarning("[StorageUI] 일부만 보관되었거나 이동할 아이템이 없습니다.");
    }

    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        Debug.Log($"[StorageUI] Toggle 호출됨 - Type: {Type}");
        UIManager.Instance?.ToggleUI(Type);
    }

    protected override void OnShow()
    {
        base.OnShow();
        RefreshUI();

        if (inventoryPanel != null && inventoryData != null && inventoryData.Container is InventoryItemData)
            inventoryPanel.Bind((InventoryItemData)inventoryData.Container);

        SoundManager.I?.PlayOpenBox();
        DragDropManager.I?.SetCurrentStorage(worldStorage);
        Debug.Log("[StorageUI] 창고 UI 열림");
    }

    protected override void OnHide()
    {
        base.OnHide();
        inventoryPanel?.Unbind();
        DragDropManager.I?.SetCurrentStorage(null);

        SoundManager.I?.PlayCloseCraftingTable();
        var player = FindFirstObjectByType<PlayerController>();
        player?.SetCurrentStorage(null);

        Debug.Log("[StorageUI] 창고 UI 닫힘");
    }
}
