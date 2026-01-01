using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SeedSlotUI : MonoBehaviour, IDragSource, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button button; 

    private ItemBase itemData;
    private int count;

    public DragSourceType SourceType => DragSourceType.SeedList;
    public int SlotIndex => -1;
    public int SeedItemId => itemData != null ? itemData.itemID : 0;
    public int Count => count;
    private ScrollRect _scroll;

    private void Awake()
    {
         _scroll = GetComponentInParent<ScrollRect>(true);
    }

    public void SetData(SeedStack stack, Action<int> onSeedClicked)
    {
        itemData = ItemDatabase.I.GetItem(stack.seedId);
        count = stack.count;

        if (itemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (icon) icon.sprite = itemData.icon;
        if (nameText) nameText.text = itemData.itemName;
        if (countText) countText.text = count.ToString();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSeedClicked?.Invoke(itemData.itemID));
        }

        gameObject.SetActive(count > 0);
    }

    public DragContext CreateDragContext(ItemBase item) => new DragContext
    {
        Item = item,
        SourceType = SourceType,
        SourceSlotIndex = SlotIndex,
        Source = this
    };

    public ItemBase GetItem() => itemData;
    public void OnDragSuccess() { }
    public void OnDragCancelled() { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null || count <= 0) return;

        var ctx = CreateDragContext(itemData);

        UIDragManager.I.BeginDrag(ctx, gameObject, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log($"[SeedSlotUI] OnDrag called");
        if (!UIDragManager.I.IsDragging) return;
        UIDragManager.I.Drag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UIDragManager.I.EndDrag(eventData);
    }
}
