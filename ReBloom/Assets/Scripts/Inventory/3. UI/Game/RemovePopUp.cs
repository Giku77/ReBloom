using System;
using UnityEngine;

public class RemovePopUp : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RemovePopUPUI removePopUPUI;
    [SerializeField] private InventoryItemData inventoryData;
    [SerializeField] private GameInventory gameInventory;
    [SerializeField] private GameInventoryUI gameInventoryUI;

    private ItemBase selectedItem;
    private int currentItemQuantity;
    private int settingQuantity;
    private DragSourceType sourceType; // 드래그 소스 타입 추가

    public event Action<ItemBase, int, Vector3> OnItemDropRequested;

    public int SettingQuantity
    {
        get => settingQuantity;
        set => settingQuantity = Mathf.Clamp(value, 1, currentItemQuantity);
    }

    public int CurrentItemQuantity
    {
        get => currentItemQuantity;
        private set => currentItemQuantity = value;
    }

    public void Awake()
    {
        removePopUPUI.gameObject.SetActive(false);
    }

    #region 수량 설정
    public void AdjustQuantity(int delta)
    {
        SettingQuantity += delta;
        removePopUPUI.UpdateQuantityUI(settingQuantity);
    }

    public void SetQuantity(int value)
    {
        SettingQuantity = value;
        removePopUPUI.UpdateQuantityUI(settingQuantity);
    }
    #endregion

    #region 아이템 제거/드롭
    public void OnRemoveItem()
    {
        if (selectedItem == null || settingQuantity <= 0)
        {
            Debug.LogWarning("[RemovePopUp] 유효하지 않은 아이템 또는 수량입니다.");
            OnClose();
            return;
        }

        // 소스 타입에 따라 다른 처리
        if (sourceType == DragSourceType.Storage)
        {
            HandleStorageRemove();
        }
        else // Inventory
        {
            HandleInventoryRemove();
        }

        OnClose();
    }

    /// <summary>
    /// 인벤토리에서 제거 후 드롭
    /// </summary>
    private void HandleInventoryRemove()
    {
        // 1. 인벤토리에서 제거
        if (gameInventory != null)
        {
            gameInventory.RemoveItem(selectedItem.itemID, settingQuantity);
            Debug.Log($"[RemovePopUp] 인벤토리에서 {selectedItem.itemName} x{settingQuantity} 제거");
        }

        // 2. 월드에 드롭 요청
        if (DragDropManager.I != null)
        {
            DragDropManager.I.DropItemFromPopup(selectedItem, settingQuantity);
            Debug.Log($"[RemovePopUp] {selectedItem.itemName} x{settingQuantity} 드롭 요청");
        }
        else
        {
            Debug.LogError("[RemovePopUp] DragDropManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 스토리지에서 제거 후 드롭
    /// </summary>
    private void HandleStorageRemove()
    {
        // 1. 현재 스토리지 가져오기
        var currentStorage = DragDropManager.I?.GetCurrentStorage();
        if (currentStorage == null)
        {
            Debug.LogError("[RemovePopUp] 현재 스토리지를 찾을 수 없습니다!");
            return;
        }

        var storageData = currentStorage.GetStorageData();
        if (storageData == null)
        {
            Debug.LogError("[RemovePopUp] StorageData를 찾을 수 없습니다!");
            return;
        }

        // 2. 스토리지에서 제거
        bool removed = storageData.RemoveItem(selectedItem.itemID, settingQuantity);

        if (removed)
        {
            Debug.Log($"[RemovePopUp] 스토리지에서 {selectedItem.itemName} x{settingQuantity} 제거");

            // 3. 월드에 드롭 요청
            if (DragDropManager.I != null)
            {
                DragDropManager.I.DropItemFromPopup(selectedItem, settingQuantity);
                Debug.Log($"[RemovePopUp] {selectedItem.itemName} x{settingQuantity} 드롭 요청");
            }
            else
            {
                Debug.LogError("[RemovePopUp] DragDropManager를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[RemovePopUp] 스토리지에서 제거 실패: {selectedItem.itemName}");
        }
    }
    #endregion

    #region 초기화 및 UI 제어
    /// <summary>
    /// 팝업 열기 (인벤토리용 - 기존 호환성 유지)
    /// </summary>
    public void OnOpen(ItemBase item)
    {
        OnOpen(item, DragSourceType.Inventory);
    }

    /// <summary>
    /// 팝업 열기 (소스 타입 지정)
    /// </summary>
    public void OnOpen(ItemBase item, DragSourceType source)
    {
        if (item == null)
        {
            Debug.LogError("[RemovePopUp] 전달된 아이템이 null입니다!");
            return;
        }

        selectedItem = item;
        sourceType = source;

        // 소스에 따라 다른 수량 가져오기
        if (sourceType == DragSourceType.Storage)
        {
            // 스토리지 수량
            var currentStorage = DragDropManager.I?.GetCurrentStorage();
            if (currentStorage == null)
            {
                Debug.LogError("[RemovePopUp] 현재 스토리지를 찾을 수 없습니다!");
                return;
            }

            var storageData = currentStorage.GetStorageData();
            currentItemQuantity = storageData.GetItemCount(selectedItem.itemID);

            if (currentItemQuantity <= 0)
            {
                Debug.LogWarning($"[RemovePopUp] {selectedItem.itemName}이(가) 스토리지에 없거나 수량이 0입니다!");
                OnClose();
                return;
            }

            Debug.Log($"[RemovePopUp] 스토리지 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
        }
        else // Inventory
        {
            if (inventoryData == null)
            {
                Debug.LogError("[RemovePopUp] InventoryData가 null입니다!");
                return;
            }

            currentItemQuantity = inventoryData.GetItemCount(selectedItem.itemID);

            if (currentItemQuantity <= 0)
            {
                Debug.LogWarning($"[RemovePopUp] {selectedItem.itemName}이(가) 인벤토리에 없거나 수량이 0입니다!");
                OnClose();
                return;
            }

            Debug.Log($"[RemovePopUp] 인벤토리 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
        }

        // UI 초기화
        if (removePopUPUI != null)
        {
            removePopUPUI.Init(selectedItem, currentItemQuantity);
        }
        else
        {
            Debug.LogError("[RemovePopUp] RemovePopUPUI가 null입니다!");
            return;
        }

        removePopUPUI.gameObject.SetActive(true);
    }

    public void OnClose()
    {
        removePopUPUI.gameObject.SetActive(false);
        selectedItem = null;
        currentItemQuantity = 0;
        settingQuantity = 1;
        sourceType = DragSourceType.Inventory; // 기본값으로 리셋

        // 인벤토리 UI만 갱신 (스토리지는 이벤트로 자동 갱신됨)
        if (gameInventoryUI != null)
        {
            gameInventoryUI.RefreshUI();
            Debug.Log("[RemovePopUp] 인벤토리 UI 갱신 완료");
        }
    }
    #endregion
}