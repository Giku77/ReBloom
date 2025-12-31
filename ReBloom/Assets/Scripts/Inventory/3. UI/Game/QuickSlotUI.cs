using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour, IItemSlot, IDragSource
{
    [Header("Ref")]
    [SerializeField] private Image slotIcon;
    [SerializeField] private Image quantityFrame;
    [SerializeField] private TextMeshProUGUI itemQuantity;
    [SerializeField] private TextMeshProUGUI itemName;

    [Header("Fallback")]
    [SerializeField] private Sprite defaultIcon;

    private ItemBase currentItem;
    private int currentQuantity;

    #region IItemSlot 구현
    public void SetItem(ItemBase item, int quantity)
    {
        OnUpdateSlotInfo(item, quantity);
    }

    public void Clear()
    {
        currentItem = null;
        currentQuantity = 0;

        if (slotIcon != null)
        {
            slotIcon.enabled = false;
        }

        if (quantityFrame != null)
        {
            quantityFrame.enabled = false;
        }

        if (itemQuantity != null)
        {
            itemQuantity.text = "";
        }

        if (itemName != null)
        {
            itemName.text = "";
        }
    }

    public ItemBase GetItem() => currentItem;
    #endregion

    #region IDragSource 구현
    public DragSourceType SourceType => DragSourceType.QuickSlot;
    public int SlotIndex => transform.parent.GetSiblingIndex();

    public DragContext CreateDragContext(ItemBase item)
    {
        return new DragContext
        {
            Item = item,
            SourceType = DragSourceType.QuickSlot,
            SourceSlotIndex = SlotIndex,
            Source = this
        };
    }

    public void OnDragSuccess()
    {
        // 퀵슬롯은 매니저가 처리하므로 여기선 로그만
        Debug.Log($"[QuickSlotUI] 슬롯 {SlotIndex} 드래그 성공");
    }

    public void OnDragCancelled()
    {
        // 취소 시 할 작업
    }
    #endregion

    /// <summary>
    /// 슬롯 정보 업데이트
    /// </summary>
    public void OnUpdateSlotInfo(ItemBase item, int quantity)
    {
       // itemName.gameObject.SetActive(true);
        currentItem = item;
        currentQuantity = quantity;

        if (item == null)
        {
            Clear();
            return;
        }

        // 아이콘
        if (slotIcon != null)
        {
            if (item.icon != null)
            {
                slotIcon.sprite = item.icon;
                slotIcon.enabled = true;
                slotIcon.color = Color.white;
            }
            else
            {
                slotIcon.sprite = defaultIcon;
                slotIcon.enabled = true;
                slotIcon.color = Color.gray;
                itemName.gameObject.SetActive(false);
                Debug.LogWarning($"[QuickSlotUI] {item.itemName} 아이콘 없음");
            }
        }

        // 수량
        if (itemQuantity != null)
        {
            itemQuantity.text = quantity > 1 ? quantity.ToString() : "";
        }
        if (quantityFrame != null)
        {
            quantityFrame.enabled = quantity > 1;
        }

        // 이름
        if (itemName != null)
        {
            itemName.text = item.itemName;
        }
    }
}