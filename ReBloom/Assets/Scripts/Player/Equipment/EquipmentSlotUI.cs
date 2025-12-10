using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDragSource, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image slotIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private GameObject emptyIndicator; // 빈 슬롯 표시 (Optional)

    [Header("Fallback")]
    [SerializeField] private Sprite defaultIcon; // 기본 아이콘 (Optional)

    private float lastClickTime = 0f;
    private float doubleClickDelay = 0.25f;

    private PlayerEquipManager equipManager;
    private GearType slotType;
    private ItemBase itemData;
    private GameInventoryToolTip tooltip;

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
    #endregion
    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            tooltip = parentCanvas.GetComponentInChildren<GameInventoryToolTip>();
            if (tooltip != null)
            {
                Debug.Log("[GameInventorySlot] 툴팁을 Canvas에서 찾았습니다.");
            }
        }
    }

    public void Initialize(PlayerEquipManager manager, GearType type)
    {
        equipManager = manager;
        slotType = type;
    }

    public void UpdateSlotInfo(ItemBase itemData)
    {
        if (itemData == null)
        {
            //Debug.Log("[EquipmentSlotUI] 아이템데이터가 없습니다.");
            ClearSlot();
            return;
        }

        this.itemData = itemData;

        if (slotIcon != null)
        {
            if (itemData.icon != null)
            {
                slotIcon.sprite = itemData.icon;
                slotIcon.enabled = true;
                slotIcon.color = Color.white;
            }
            else
            {
                // 아이콘이 없으면 기본 아이콘 사용
                if (defaultIcon != null)
                {
                    slotIcon.sprite = defaultIcon;
                    slotIcon.enabled = true;
                    slotIcon.color = Color.gray;
                    Debug.LogWarning($"[EquipmentSlotUI] {itemData.itemName} 아이콘이 없어 기본 아이콘 사용");
                }
                else
                {
                    slotIcon.enabled = false;
                    Debug.LogWarning($"[EquipmentSlotUI] {itemData.itemName} 아이콘 없음 (비활성화)");
                }
            }
        }

        // 아이템 이름 설정
        if (itemName != null)
        {
            itemName.text = itemData.itemName;
            itemName.enabled = true;
        }

        // 빈 슬롯 표시 숨기기
        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(false);
        }

        Debug.Log($"[EquipmentSlotUI] 장비 UI 업데이트 완료: {itemData.itemName}");
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void ClearSlot()
    {
        if (slotIcon != null)
        {
            slotIcon.enabled = false;
        }

        if (itemName != null)
        {
            itemName.text = "";
            itemName.enabled = false;
        }

        // 빈 슬롯 표시 활성화
        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(true);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        float now = Time.time;
        if (now - lastClickTime <= doubleClickDelay)
        {
            OnDoubleClick();
        }

        lastClickTime = now;
    }

    private void OnDoubleClick()
    {
        if (equipManager == null) return;

        equipManager.UnEquip(slotType); 
    }

    public void OnDragSuccess()
    {
        Debug.Log("[EquipmentSlotUI] 드래그 성공");
    }

    public void OnDragCancelled()
    {
        
    }

    #region 툴팁
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[EquipMentSlotUI] itemData: {itemData?.itemName ?? "null"}");

        if (tooltip != null && itemData != null)
        {
            Debug.Log($"[OnPointerEnter] 툴팁 Show 호출!");
            tooltip.Show(itemData);
        }
        else
        {
            Debug.Log($"[OnPointerEnter] tooltip: {tooltip != null}, itemData: {itemData != null}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[OnPointerExit] 호출됨");
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
    #endregion
}