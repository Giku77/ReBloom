using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;
using UnityEngine.UI;

/// <summary>
/// 게임 인벤토리의 아이템 슬롯
/// </summary>
public class GameInventorySlot : MonoBehaviour, IItemSlot, IDragSource, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image quantityFrame;
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Header("Optional")]
    [SerializeField] private DebugItemTooltip tooltip;

    private ItemBase itemData;

    #region IItemSlot 구현
    public void SetItem(ItemBase item, int quantity)
    {
        itemData = item;

        if (item != null)
        {
            // 아이콘
            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
                itemIcon.color = Color.white;
            }

            // 수량
            if (quantityText != null)
            {
                if (quantityFrame != null)
                {
                    quantityFrame.enabled = quantity > 1;
                }
                quantityText.text = quantity > 1 ? quantity.ToString() : "";
            }
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName;
            }
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        itemData = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
        }
    }

    public ItemBase GetItem() => itemData;
    #endregion

    #region IDragSource 구현
    public DragSourceType SourceType => DragSourceType.Inventory;
    public int SlotIndex => transform.GetSiblingIndex();

    public DragContext CreateDragContext(ItemBase item)
    {
        return new DragContext
        {
            Item = item,
            SourceType = this.SourceType,
            SourceSlotIndex = SlotIndex,
            Source = this
        };
    }

    public void OnDragSuccess()
    {
        // 인벤토리 UI 새로고침
        var inventoryUI = GetComponentInParent<GameInventoryUI>();
        inventoryUI?.RefreshUI();

        Debug.Log("[GameInventorySlot] 드래그 성공 - UI 갱신");
    }

    public void OnDragCancelled()
    {
        // 취소 시 특별히 할 작업 없음
    }
    #endregion

    #region 툴팁 (아직 적용 안함)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && itemData != null)
        {
            tooltip.Show(itemData, showDescription: false, showStats: false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
    #endregion
}