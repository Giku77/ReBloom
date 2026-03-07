using System;
using UnityEngine;

public class RemovePopUp : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RemovePopUPUI removePopUPUI;
    [SerializeField] private GameInventory gameInventory;
    [SerializeField] private GameInventoryUI gameInventoryUI;

    private ItemBase selectedItem;
    private int currentItemQuantity;
    private int settingQuantity;
    private DragSourceType sourceType;

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
        if (gameInventory == null)
            gameInventory = FindFirstObjectByType<GameInventory>();

        removePopUPUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null)
            return;

        var inventory = playerObj.GetComponent<GameInventory>();
        if (inventory != null)
            gameInventory = inventory;
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

        if (sourceType == DragSourceType.Storage)
            HandleStorageRemove();
        else
            HandleInventoryRemove();

        OnClose();
    }

    private void HandleInventoryRemove()
    {
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

    private void HandleStorageRemove()
    {
        var currentStorage = DragDropManager.I?.GetCurrentStorage();
        if (currentStorage == null)
        {
            Debug.LogError("[RemovePopUp] 현재 스토리지를 찾을 수 없습니다!");
            return;
        }

        if (DragDropManager.I != null)
        {
            DragDropManager.I.DropItemFromPopup(selectedItem, settingQuantity);
            Debug.Log($"[RemovePopUp] 스토리지 드롭 요청: {selectedItem.itemName} x{settingQuantity}");
        }
        else
        {
            Debug.LogError("[RemovePopUp] DragDropManager를 찾을 수 없습니다!");
        }
    }
    #endregion

    #region 초기화 및 UI 제어
    public void OnOpen(ItemBase item)
    {
        OnOpen(item, DragSourceType.Inventory);
    }

    public void OnOpen(ItemBase item, DragSourceType source)
    {
        if (item == null)
        {
            Debug.LogError("[RemovePopUp] 전달된 아이템이 null입니다!");
            return;
        }

        selectedItem = item;
        sourceType = source;

        if (sourceType == DragSourceType.Storage)
        {
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

            currentItemQuantity = storageData.GetItemCount(selectedItem.itemID);

            if (currentItemQuantity <= 0)
            {
                Debug.LogWarning($"[RemovePopUp] {selectedItem.itemName}이(가) 스토리지에 없거나 수량이 0입니다!");
                OnClose();
                return;
            }

            Debug.Log($"[RemovePopUp] 스토리지 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
        }
        else
        {
            if (gameInventory == null)
            {
                Debug.LogError("[RemovePopUp] GameInventory가 null입니다!");
                return;
            }

            currentItemQuantity = gameInventory.GetItemCount(selectedItem.itemID);

            if (currentItemQuantity <= 0)
            {
                Debug.LogWarning($"[RemovePopUp] {selectedItem.itemName}이(가) 인벤토리에 없거나 수량이 0입니다!");
                OnClose();
                return;
            }

            Debug.Log($"[RemovePopUp] 인벤토리 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
        }

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
        sourceType = DragSourceType.Inventory;

        if (gameInventoryUI != null)
        {
            gameInventoryUI.RefreshUI();
            Debug.Log("[RemovePopUp] 인벤토리 UI 갱신 완료");
        }
    }
    #endregion
}
