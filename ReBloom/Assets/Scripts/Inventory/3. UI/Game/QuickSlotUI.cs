using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour, IItemSlot, IDragSource
{
    [Header("Ref")]
    [SerializeField] Image slotIcon;
    [SerializeField] TextMeshProUGUI itemQuantity;
    [SerializeField] TextMeshProUGUI itemName;

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
    public int SlotIndex => transform.GetSiblingIndex();

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
        // 퀵슬롯 매니저에 알림
        var quickSlot = GetComponentInParent<QuickSlot>();
        if (quickSlot != null)
        {
            // 필요시 퀵슬롯 갱신
            Debug.Log("[QuickSlotUI] 드래그 성공");
        }
    }

    public void OnDragCancelled()
    {
        // 취소 시 할 작업
    }
    #endregion

    // 기존 메서드
    public void OnUpdateSlotInfo(ItemBase item, int quantity)
    {
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
                Debug.LogWarning($"[QuickSlotUI] {item.itemName} 아이콘 없음");
            }
        }

        // 수량
        if (itemQuantity != null)
        {
            itemQuantity.text = quantity > 1 ? quantity.ToString() : "";
        }

        // 이름
        if (itemName != null)
        {
            itemName.text = item.itemName;
        }
    }

    //[Header("Fallback")]
    //[SerializeField] private Sprite defaultIcon;

    //private ItemBase currentItem;
    //private int currentQuantity;

    //#region IItemSlot 구현
    //public void SetItem(ItemBase item, int quantity)
    //{
    //    OnUpdateSlotInfo(item, quantity);
    //}

    //public void Clear()
    //{
    //    currentItem = null;
    //    currentQuantity = 0;

    //    if (slotIcon != null)
    //    {
    //        slotIcon.enabled = false;
    //    }

    //    if (itemQuantity != null)
    //    {
    //        itemQuantity.text = "";
    //    }

    //    if (itemName != null)
    //    {
    //        itemName.text = "";
    //    }
    //}

    //public ItemBase GetItem() => currentItem;
    //#endregion

    //#region IDragSource 구현
    //public DragSourceType SourceType => DragSourceType.QuickSlot;
    //public int SlotIndex => transform.GetSiblingIndex();

    //public DragContext CreateDragContext(ItemBase item)
    //{
    //    return new DragContext
    //    {
    //        Item = item,
    //        SourceType = DragSourceType.QuickSlot,
    //        SourceSlotIndex = SlotIndex,
    //        Source = this
    //    };
    //}

    //public void OnDragSuccess()
    //{
    //    // 퀵슬롯 매니저에 알림
    //    var quickSlot = GetComponentInParent<QuickSlot>();
    //    if (quickSlot != null)
    //    {
    //        // 필요시 퀵슬롯 갱신
    //        Debug.Log("[QuickSlotUI] 드래그 성공");
    //    }
    //}

    //public void OnDragCancelled()
    //{
    //    // 취소 시 할 작업
    //}
    //#endregion

    //public void OnUpdateSlotInfo(ItemBase item, int quantity)
    //{
    //    currentItem = item;
    //    currentQuantity = quantity;

    //    if (item == null)
    //    {
    //        Clear();
    //        return;
    //    }

    //    // 아이콘
    //    if (slotIcon != null)
    //    {
    //        if (item.icon != null)
    //        {
    //            slotIcon.sprite = item.icon;
    //            slotIcon.enabled = true;
    //            slotIcon.color = Color.white;
    //        }
    //        else
    //        {
    //            slotIcon.sprite = defaultIcon;
    //            slotIcon.enabled = true;
    //            slotIcon.color = Color.gray;
    //            Debug.LogWarning($"[QuickSlotUI] {item.itemName} 아이콘 없음");
    //        }
    //    }

    //    // 수량
    //    if (itemQuantity != null)
    //    {
    //        itemQuantity.text = quantity > 1 ? quantity.ToString() : "";
    //    }

    //    // 이름
    //    if (itemName != null)
    //    {
    //        itemName.text = item.itemName;
    //    }
    //}
}