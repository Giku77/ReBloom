using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;
using UnityEngine.UI;

/// <summary>
/// 게임 인벤토리의 아이템 슬롯
/// </summary>
public class GameInventorySlot : MonoBehaviour, IItemSlot, IDragSource,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundColor;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image quantityFrame;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private GameObject hoverPrefab;
    private QuickSlot quickSlot;

    [Header("Optional")]
    [SerializeField] private GameInventoryToolTip tooltip;

    //[Header("Material Settings")]
    //[SerializeField] private Material overlayMaterial;

    private ItemBase itemData;

    #region Unity Lifecycle
    private void Awake()
    {
        inventory = FindFirstObjectByType<GameInventory>();
        quickSlot = FindFirstObjectByType<QuickSlot>();

        if (inventory == null)
        {
            Debug.LogWarning("[GameInventorySlot] GameInventory를 찾을 수 없습니다!");
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            tooltip = parentCanvas.GetComponentInChildren<GameInventoryToolTip>();
            if (tooltip != null)
            {
                Debug.Log("[GameInventorySlot] 툴팁을 Canvas에서 찾았습니다.");
            }
        }
        if (tooltip != null)
        {
            if (tooltip.QuickslotButton != null)
            {
                tooltip.QuickslotButton.onClick.RemoveListener(OnClickTooltipQuickslot);
                tooltip.QuickslotButton.onClick.AddListener(OnClickTooltipQuickslot);
            }

            if (tooltip.UseButton != null)
            {
                tooltip.UseButton.onClick.RemoveListener(OnClickTooltipUse);
                tooltip.UseButton.onClick.AddListener(OnClickTooltipUse);
            }
        }
    }
    #endregion

    private void OnClickTooltipUse()
    {
        if (itemData != null && (itemData.canUseable || itemData.canEquip))
            inventory?.Consume(itemData.itemID, 1);
    }

    private void OnClickTooltipQuickslot()
    {
        if (itemData == null || !itemData.canQuickSlot) return;

        if (quickSlot == null)
        {
            Debug.LogWarning("[GameInventorySlot] QuickSlot을 찾을 수 없음");
            return;
        }

        bool ok = quickSlot.TryAssignFromInventory(itemData.itemID); 
        Debug.Log(ok
            ? $"[GameInventorySlot] {itemData.itemName} 퀵슬롯 자동 등록 성공"
            : $"[GameInventorySlot] {itemData.itemName} 퀵슬롯 자동 등록 실패");
    }

    #region IItemSlot 구현
    public void SetItem(ItemBase item, int quantity)
    {
        itemData = item;

        if (item != null)
        {
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
            // 아이콘
            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
                itemIcon.color = Color.white;
                itemNameText.gameObject.SetActive(false);
            }
            Color color = GameInventoryToolTip.GetTierColor(itemData.tier); ;
            color.a = 0.1f;
            backgroundColor.color = color;
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
    public int SlotIndex
    {
        get
        {
            var marker = GetComponentInParent<DropZoneMarker>();
            if (marker != null)
            {
                return marker.SlotIndex;
            }

            // Fallback
            return transform.parent.GetSiblingIndex();
        }
    }

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

        //Debug.Log("[GameInventorySlot] 드래그 성공 - UI 갱신");
    }

    public void OnDragCancelled()
    {
        // 취소 시 특별히 할 작업 없음
    }
    #endregion

    #region 툴팁
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[OnPointerEnter] itemData: {itemData?.itemName ?? "null"}");

        if (tooltip != null && itemData != null)
        {
            Debug.Log($"[OnPointerEnter] 툴팁 Show 호출!");
            tooltip.Show(itemData);
            hoverPrefab.SetActive(true);
        }
        else
        {
            Debug.Log($"[OnPointerEnter] tooltip: {tooltip != null}, itemData: {itemData != null}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[OnPointerExit] 호출됨");
        if (tooltip != null && PlatformManager.Instance.IsPC)
        {
            tooltip.Hide();
        }
        hoverPrefab.SetActive(false);
    }

    private void OnDisable()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
        hoverPrefab.SetActive(false);
    }
    #endregion

    #region 더블클릭 처리

    [Header("Double Click Settings")]
    [SerializeField] private float doubleClickDelay = 0.25f;
    private GameInventory inventory;
    private float lastClickTime = 0f;
    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭만 처리
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        float now = Time.time;

        // 더블클릭 감지
        if (now - lastClickTime <= doubleClickDelay)
        {
            OnDoubleClick();
        }

        lastClickTime = now;
    }

    private void OnDoubleClick()
    {
        if (itemData == null || inventory == null)
            return;

        // 사용 가능한 아이템만 사용
        if (itemData.canUseable || itemData.canEquip)
        {
            Debug.Log($"[GameInventorySlot] {itemData.itemName} 더블클릭 사용");
            inventory.Consume(itemData.itemID, 1);
        }
        else
        {
            Debug.Log($"[GameInventorySlot] {itemData.itemName}은(는) 사용할 수 없는 아이템입니다.");
        }
    }
    #endregion
}