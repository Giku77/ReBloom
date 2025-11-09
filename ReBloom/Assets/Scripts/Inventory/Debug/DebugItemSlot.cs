using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 디버그 인벤토리의 개별 아이템 슬롯
/// </summary>
public class DebugItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgTierBar;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtID;
    [SerializeField] private LayoutElement layoutElement;

    private ItemBase itemData;
    private DebugItemTooltip tooltip;
    private bool showDescription;
    private bool showStats;

    public void Initialize(ItemBase item, DebugItemTooltip tooltipUI)
    {
        itemData = item;
        tooltip = tooltipUI;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (itemData == null) return;

        // 아이콘
        if (imgIcon != null)
        {
            if (itemData.icon != null)
            {
                imgIcon.sprite = itemData.icon;
                imgIcon.color = Color.white;
            }
            else
            {
                imgIcon.sprite = null;
                imgIcon.color = new Color(0.2f, 0.2f, 0.2f);
            }
        }

        // 티어 색상 바
        if (imgTierBar != null)
        {
            imgTierBar.color = GetTierColor(itemData.tier);
        }

        // 이름
        if (txtName != null)
        {
            txtName.text = itemData.itemName;
        }

        // ID
        if (txtID != null)
        {
            txtID.text = $"ID: {itemData.itemID}";
        }
    }

    public void SetIconSize(float size)
    {
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = size;
            layoutElement.preferredHeight = size + 40;
        }
    }

    public void SetShowDescription(bool show)
    {
        showDescription = show;
    }

    public void SetShowStats(bool show)
    {
        showStats = show;
    }

    #region 호버 이벤트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && itemData != null)
        {
            tooltip.Show(itemData, showDescription, showStats);
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

    private Color GetTierColor(int tier)
    {
        return tier switch
        {
            1 => new Color(0.7f, 0.7f, 0.7f),      // 일반 - 회색
            2 => new Color(0.3f, 0.6f, 1f),        // 희귀 - 파랑
            3 => new Color(0.8f, 0.3f, 1f),        // 영웅 - 보라
            _ => Color.white
        };
    }
}