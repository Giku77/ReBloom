using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageDropZone : MonoBehaviour,
    IDropTarget, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private InventoryItemData inventoryData;
    [SerializeField] private Image background;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f);

    private WorldStorage currentStorage;
    private bool isPointerOver;

    /// <summary>
    /// StorageUI에서 호출하여 현재 창고 설정
    /// </summary>
    public void Initialize(WorldStorage storage)
    {
        currentStorage = storage;
        Debug.Log($"[StorageDropZone] 초기화 완료 - Storage: {storage?.name}");
    }

    #region IDropTarget 구현
    public bool CanAcceptDrop(DragContext context)
    {
        if (currentStorage == null)
        {
            Debug.LogWarning("[StorageDropZone] WorldStorage가 설정되지 않았습니다!");
            return false;
        }

        return context?.Item != null && context.IsFromInventory;
    }

    public void HandleDrop(DragContext context)
    {
        if (currentStorage == null)
        {
            Debug.LogError("[StorageDropZone] WorldStorage가 설정되지 않았습니다!");
            return;
        }

        Debug.Log($"[StorageDropZone] HandleDrop 시작: {context.Item.itemName}");

        int quantity = inventoryData.GetItemCount(context.Item.itemID);
        Debug.Log($"[StorageDropZone] 인벤토리 수량: {quantity}");

        if (quantity <= 0)
        {
            Debug.LogWarning($"[StorageDropZone] 인벤토리에 아이템이 없습니다!");
            return;
        }

        bool removed = inventoryData.RemoveItem(context.Item.itemID, quantity);
        Debug.Log($"[StorageDropZone] 인벤토리 제거 결과: {removed}");

        if (removed)
        {
            currentStorage.AddItem(context.Item, quantity);
            Debug.Log($"[StorageDropZone] {context.Item.itemName} {quantity}개 창고에 보관 완료!");
        }
    }
    #endregion

    #region Unity EventSystem
    public void OnDrop(PointerEventData eventData)
    {
        var context = ItemIconDragHandler.CurrentContext;

        Debug.Log($"[StorageDropZone] OnDrop 호출 - Context: {context?.Item?.itemName}");

        if (CanAcceptDrop(context))
        {
            HandleDrop(context);
        }
        else
        {
            Debug.LogWarning("[StorageDropZone] 드롭 불가");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (background != null && ItemIconDragHandler.CurrentContext != null)
        {
            var context = ItemIconDragHandler.CurrentContext;

            if (CanAcceptDrop(context))
            {
                background.color = hoverColor;
            }
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