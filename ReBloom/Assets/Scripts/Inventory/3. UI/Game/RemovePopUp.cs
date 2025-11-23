using System;
using UnityEngine;

public class RemovePopUp : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RemovePopUPUI removePopUPUI;
    [SerializeField] private InventoryItemData inventoryData;
    [SerializeField] private WorldDropZone worldDropZone;
    [SerializeField] private GameInventory gameInventory;

    private ItemBase selectedItem;
    private int currentItemQuantity;
    private int settingQuantity;

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

        if (worldDropZone != null)
        {
            worldDropZone.DropItemFromPopup(selectedItem, settingQuantity);
            Debug.Log($"[RemovePopUp] {selectedItem.itemName} x{settingQuantity} 드롭 요청");
        }
        else
        {
            Debug.LogError("[RemovePopUp] WorldDropZone이 할당되지 않았습니다!");
        }

        if (gameInventory != null)
        {
            gameInventory.RemoveItem(selectedItem.itemID, settingQuantity);
        }

        OnClose();
    }
    #endregion

    #region 초기화 및 UI 제어
    public void OnOpen(ItemBase item)
    {
        if (item == null)
        {
            Debug.LogError("[RemovePopUp] 전달된 아이템이 null입니다!");
            return;
        }

        selectedItem = item;

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

        //// 설정 수량 초기화
        //settingQuantity = 1;
        //removePopUPUI.UpdateQuantityUI(settingQuantity);

        Debug.Log($"[RemovePopUp] 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
    }

    public void OnClose()
    {
        removePopUPUI.gameObject.SetActive(false);
        selectedItem = null;
        currentItemQuantity = 0;
        settingQuantity = 1;
    }
    #endregion
}