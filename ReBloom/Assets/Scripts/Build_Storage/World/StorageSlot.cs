using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 창고 슬롯 - 창고 아이템 표시 및 드래그/드롭 처리
/// </summary>
public class StorageSlot : MonoBehaviour,
    IDragSource, IDropTarget, IItemSlot,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject emptyIndicator;

    [Header("Settings")]
    [SerializeField] private float doubleClickDelay = 0.25f;

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
        // 인벤토리/퀵슬롯에서만 받을 수 있음 (디버그 불가)
        if (context?.Item == null || context.IsFromDebug)
            return false;

        // 같은 슬롯이면 불가
        if (context.SourceType == DragSourceType.Storage &&
            context.SourceSlotIndex == SlotIndex)
            return false;

        // 이미 아이템이 있으면 같은 종류만 받을 수 있음
        if (itemData != null && itemData.itemID != context.Item.itemID)
            return false;

        return true;
    }

    public void HandleDrop(DragContext context)
    {
        Debug.Log($"[StorageSlot] 아이템 드롭: {context.Item.itemName}");

        // 창고 슬롯은 직접 처리하지 않고 StorageUI가 처리하도록 위임
        // (StorageUI가 데이터 동기화 담당)
    }
    #endregion

    #region IItemSlot 구현
    public void SetItem(ItemBase item, int itemQuantity)
    {
        itemData = item;
        quantity = itemQuantity;
        UpdateUI();
    }

    public void Clear()
    {
        itemData = null;
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
            }
        }

        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(!hasItem);
        }
    }

    public void OnUpdateSlotInfo(ItemBase item, int itemQuantity)
    {
        SetItem(item, itemQuantity);
    }
    #endregion

    #region 더블클릭 - 인벤토리로 회수
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

        // StorageUI에게 회수 요청
        var storageUI = GetComponentInParent<StorageUI>();
        if (storageUI != null)
        {
            storageUI.WithdrawItem(SlotIndex);
        }
    }
    #endregion

    #region 툴팁
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData != null)
        {
            // TODO: 툴팁 표시
            // TooltipManager.Instance.Show(itemData);
            Debug.Log($"[StorageSlot] 툴팁: {itemData.itemName}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // TODO: 툴팁 숨김
        // TooltipManager.Instance.Hide();
    }
    #endregion
}