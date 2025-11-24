using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 보관창고 드롭존 - 인벤토리/퀵슬롯에서 창고로 아이템 이동
/// </summary>
public class StorageDropZone : MonoBehaviour,
    IDropTarget, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private WorldStorage storageInventory;
    [SerializeField] private InventoryItemData inventoryData;
    [SerializeField] private Image background;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f);

    private bool isPointerOver;

    #region IDropTarget 구현
    public bool CanAcceptDrop(DragContext context)
    {
        // 인벤토리에서만 가능 (디버그는 불가)
        return context?.Item != null && context.IsFromInventory;
    }

    public void HandleDrop(DragContext context)
    {
        if (storageInventory == null)
        {
            Debug.LogError("[StorageDropZone] StorageInventory가 없습니다!");
            return;
        }

        // 인벤토리에서 아이템 제거
        int quantity = inventoryData.GetItemCount(context.Item.itemID);
        inventoryData.RemoveItem(context.Item.itemID, quantity);

        if (quantity > 0)
        {
            // 창고에 추가
            storageInventory.AddItem(context.Item, quantity);
            Debug.Log($"[StorageDropZone] {context.Item.itemName} {quantity}개 보관");
        }
    }
    #endregion

    #region Unity EventSystem
    public void OnDrop(PointerEventData eventData)
    {
        var context = ItemIconDragHandler.CurrentContext;

        if (CanAcceptDrop(context))
        {
            HandleDrop(context);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        if (background != null && ItemIconDragHandler.CurrentContext != null)
        {
            background.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        if (background != null)
        {
            background.color = normalColor;
        }
    }
    #endregion
}