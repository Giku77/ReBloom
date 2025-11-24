using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StorageSlot : MonoBehaviour,
    IDragSource, IDropTarget, IItemSlot,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private Image background;

    [Header("Settings")]
    [SerializeField] private float doubleClickDelay = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 1f, 1f);

    private ItemBase itemData;
    private int quantity;
    private float lastClickTime;

    #region IDragSource 구현
    public DragSourceType SourceType => DragSourceType.Storage;
    public int SlotIndex => transform.GetSiblingIndex();

    public DragContext CreateDragContext(ItemBase item)
    {
        return new DragContext
        {
            Item = item,
            SourceType = SourceType,
            SourceSlotIndex = SlotIndex,
            Source = this
        };
    }

    public void OnDragSuccess()
    {
        Debug.Log($"[StorageSlot] 드래그 성공: {itemData?.itemName}");
    }

    public void OnDragCancelled()
    {
        Debug.Log($"[StorageSlot] 드래그 취소");
    }

    public ItemBase GetItem() => itemData;
    #endregion

    #region IDropTarget 구현
    public bool CanAcceptDrop(DragContext context)
    {
        if (context?.Item == null || context.IsFromDebug)
            return false;

        if (context.SourceType == DragSourceType.Storage &&
            context.SourceSlotIndex == SlotIndex)
            return false;

        if (itemData != null && itemData.itemID != context.Item.itemID)
            return false;

        return true;
    }

    public void HandleDrop(DragContext context)
    {
        Debug.Log($"[StorageSlot] 아이템 드롭: {context.Item.itemName}");
    }
    #endregion

    #region IItemSlot 구현
    public void SetItem(ItemBase item, int itemQuantity)
    {
        itemData = item;
        itemName.text = item.itemName;
        quantity = itemQuantity;
        UpdateUI();
    }

    public void Clear()
    {
        itemData = null;
        itemName.text = "";
        quantity = 0;
        UpdateUI();
    }

    public bool IsEmpty() => itemData == null;
    #endregion

    #region UI 업데이트
    private void UpdateUI()
    {
        bool hasItem = itemData != null && quantity > 0;

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            if (hasItem)
            {
                iconImage.sprite = itemData.icon;
            }
        }

        if (quantityText != null)
        {
            quantityText.enabled = hasItem && quantity > 1;
            if (hasItem)
            {
                quantityText.text = quantity.ToString();
                itemName.text = itemData.itemName;
            }
        }

        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(!hasItem);
        }

        if (background != null)
        {
            background.color = normalColor;
        }
    }

    public void OnUpdateSlotInfo(ItemBase item, int itemQuantity)
    {
        SetItem(item, itemQuantity);
    }
    #endregion

    #region 더블클릭
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData == null) return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickDelay)
        {
            OnDoubleClick();
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.time;
        }
    }

    private void OnDoubleClick()
    {
        if (itemData == null) return;

        Debug.Log($"[StorageSlot] 더블클릭: {itemData.itemName} 인벤토리로 회수");

        var storageUI = GetComponentInParent<StorageUI>();
        if (storageUI != null)
        {
            storageUI.WithdrawItem(SlotIndex);
        }
    }
    #endregion

    #region 툴팁 & 호버
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData != null)
        {
            if (background != null)
            {
                background.color = hoverColor;
            }

            Debug.Log($"[StorageSlot] 툴팁: {itemData.itemName}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null)
        {
            background.color = normalColor;
        }
    }
    #endregion
}