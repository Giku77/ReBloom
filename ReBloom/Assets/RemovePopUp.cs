using System;
using UnityEngine;
using UnityEngine.UI;

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
        set
        {
            settingQuantity = Mathf.Clamp(value, 1, currentItemQuantity);
        }
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
    /// <summary>
    /// 설정 수량 변경 (증감값)
    /// </summary>
    public void AdjustQuantity(int delta)
    {
        SettingQuantity += delta; // 프로퍼티의 Clamp 적용됨
        removePopUPUI.UpdateQuantityUI(settingQuantity);
    }

    /// <summary>
    /// 설정 수량 직접 설정 (절대값) - 슬라이더용
    /// </summary>
    public void SetQuantity(int value)
    {
        SettingQuantity = value;
        removePopUPUI.UpdateQuantityUI(settingQuantity);
    }
    #endregion

    #region 아이템 제거/드롭
    /// <summary>
    /// 아이템 제거 실행 (월드에 드롭)
    /// </summary>
    public void OnRemoveItem()
    {
        if (selectedItem == null || settingQuantity <= 0)
        {
            Debug.LogWarning("[RemovePopUp] 유효하지 않은 아이템 또는 수량입니다.");
            OnClose();
            return;
        }

        // WorldDropZone에 드롭 요청
        if (worldDropZone != null)
        {
            // 드롭 위치 계산 (WorldDropZone에서 계산)
            worldDropZone.DropItemFromPopup(selectedItem, settingQuantity);

            Debug.Log($"[RemovePopUp] {selectedItem.itemName} x{settingQuantity} 드롭 요청");
        }
        else
        {
            Debug.LogError("[RemovePopUp] WorldDropZone이 할당되지 않았습니다!");
        }

        // 인벤토리에서 제거
        if (gameInventory != null)
        {
            gameInventory.RemoveItem(selectedItem.itemID, settingQuantity);
        }

        OnClose();
    }
    #endregion

    #region 초기화 및 UI 제어
    /// <summary>
    /// 외부에서 아이템을 전달받아 팝업 열기
    /// </summary>
    public void OnOpen(ItemBase item)
    {
        if (item == null)
        {
            Debug.LogError("[RemovePopUp] 전달된 아이템이 null입니다!");
            return;
        }

        selectedItem = item;

        // 인벤토리에서 현재 수량 가져오기
        if (inventoryData == null)
        {
            Debug.LogError("[RemovePopUp] InventoryData가 null입니다!");
            return;
        }

        if (!inventoryData.Items.TryGetValue(selectedItem.itemID, out currentItemQuantity))
        {
            Debug.LogError($"[RemovePopUp] {selectedItem.itemName}이(가) 인벤토리에 없습니다!");
            OnClose();
            return;
        }

        if (currentItemQuantity <= 0)
        {
            Debug.LogWarning($"[RemovePopUp] {selectedItem.itemName}의 수량이 0입니다!");
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

        // 팝업 활성화
        removePopUPUI.gameObject.SetActive(true);

        Debug.Log($"[RemovePopUp] 팝업 열림: {selectedItem.itemName} (수량: {currentItemQuantity})");
    }

    public void OnClose()
    {
        removePopUPUI.gameObject.SetActive(false);
    }
    #endregion
}