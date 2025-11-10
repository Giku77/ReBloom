using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 아이템 호버 시 표시되는 툴팁
/// </summary>
public class DebugItemTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtID;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtStats;
    [SerializeField] private Image imgTierBorder;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, -10);
    [SerializeField] private float followSpeed = 10f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private ItemBase currentItem;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        Hide();
    }

    private void Update()
    {
        if (tooltipRoot.activeSelf)
        {
            UpdatePosition();
        }
    }

    public void Show(ItemBase item, bool showDescription, bool showStats)
    {
        if (item == null) return;

        currentItem = item;
        tooltipRoot.SetActive(true);

        // 제목 (티어 색상 적용)
        if (txtTitle != null)
        {
            Color tierColor = GetTierColor(item.tier);
            txtTitle.text = $"<color=#{ColorUtility.ToHtmlStringRGB(tierColor)}>{item.itemName}</color>";
        }

        // ID
        if (txtID != null)
        {
            txtID.text = $"ID: {item.itemID} | 티어 {item.tier}";
        }

        // 설명
        if (txtDescription != null)
        {
            txtDescription.gameObject.SetActive(showDescription && !string.IsNullOrEmpty(item.description));
            if (showDescription)
                txtDescription.text = item.description;
        }
        else
        {
            txtDescription.gameObject.SetActive(showDescription && !string.IsNullOrEmpty(item.description));
            if (showDescription)
                txtDescription.text = "설명 없음";
        }

        // 스탯
        if (txtStats != null)
        {
            txtStats.gameObject.SetActive(showStats);
            if (showStats)
                txtStats.text = BuildStatsText(item);
        }

        // 테두리 색상
        if (imgTierBorder != null)
        {
            imgTierBorder.color = GetTierColor(item.tier);
        }

        UpdatePosition();
    }

    public void Hide()
    {
        tooltipRoot.SetActive(false);
        currentItem = null;
    }

    private void UpdatePosition()
    {
        if (parentCanvas == null) return;

        // 새 Input System 방식 사용
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePos,
            parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 targetPos = localPoint + offset;

        // 화면 밖으로 나가지 않도록 클램프
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 tooltipSize = rectTransform.sizeDelta;

        float minX = -canvasRect.rect.width / 2;
        float maxX = canvasRect.rect.width / 2 - tooltipSize.x;
        float minY = -canvasRect.rect.height / 2 + tooltipSize.y;
        float maxY = canvasRect.rect.height / 2;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        rectTransform.localPosition = Vector2.Lerp(
            rectTransform.localPosition,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }

    private string BuildStatsText(ItemBase item)
    {
        string stats = "<b>상세 정보</b>\n";
        stats += $"슬롯: {GetSlotTypeName(item.slotType)}\n";
        stats += $"최대 개수: {item.maxCount}\n";
        stats += $"퀵슬롯: {(item.canQuickSlot ? "가능" : "불가")}\n";

        if (item is ConsumableItemData)
        {
            stats += "\n<b>소비 아이템</b>\n";
            stats += "사용 시 스탯 회복";
        }
        else if (item is ProtectiveItemData protective)
        {
            stats += "\n<b>보호구</b>\n";
            stats += $"타입: {protective.gearType}";
        }

        return stats;
    }

    private Color GetTierColor(int tier)
    {
        return tier switch
        {
            1 => new Color(0.7f, 0.7f, 0.7f),
            2 => new Color(0.3f, 0.6f, 1f),
            3 => new Color(0.8f, 0.3f, 1f),
            _ => Color.white
        };
    }

    private string GetSlotTypeName(InventorySlotType type)
    {
        return type switch
        {
            InventorySlotType.Equipment => "장비",
            InventorySlotType.Consumable => "소비",
            InventorySlotType.Misc => "기타",
            InventorySlotType.Important => "중요",
            _ => "알 수 없음"
        };
    }
}