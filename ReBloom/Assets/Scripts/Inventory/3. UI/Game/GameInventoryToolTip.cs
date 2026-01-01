using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class StatUIData
{
    public StatType type;
    public GameObject container;
    public TextMeshProUGUI valueText;  // 값 표시용
    public TextMeshProUGUI label;
    public string labelText;
    public bool isNegative;            // 갈증/허기처럼 감소값인지
}

/// <summary>
/// 아이템 호버 시 표시되는 툴팁_Game용
/// </summary>
public class GameInventoryToolTip : MonoBehaviour
{
    [SerializeField] private StatUIData[] statUIs;

    [Header("UI References")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI stats;
    [SerializeField] private TextMeshProUGUI category;
    [SerializeField] private TextMeshProUGUI tier;
    [SerializeField] private Image imgBorder;
    [SerializeField] private Button useButton;
    [SerializeField] private Button quickslotButton;

    [Header("Position Settings")]
    [SerializeField] private Vector2 offset = new Vector2(120f, 0f);  // 슬롯 중심에서의 오프셋
    [SerializeField] private float padding = 10f;  // 화면 가장자리 여백
    [SerializeField] private bool useFixedPosition = true;  // true: 슬롯 기준 고정, false: 마우스 따라다님
    [SerializeField] private float followSpeed = 10f;  // 마우스 따라다닐 때 속도

    [Header("Fixed Anchor (Mobile)")]
    [SerializeField] private RectTransform fixedAnchor;    
    [SerializeField] private Vector2 fixedAnchoredPos = new Vector2(-30f, 0f); 

    private QuickSlot quickSlot;
    private GameInventory inventory;

    public Button UseButton => useButton;
    public Button QuickslotButton => quickslotButton;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private ItemBase currentItem;
    private RectTransform currentSlotRect;  // 현재 호버 중인 슬롯

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
            canvasRect = parentCanvas.GetComponent<RectTransform>();

        inventory = FindFirstObjectByType<GameInventory>();
        quickSlot = FindFirstObjectByType<QuickSlot>();

        if (useButton != null)
            useButton.onClick.AddListener(OnClickUse);

        if (quickslotButton != null)
            quickslotButton.onClick.AddListener(OnClickQuickslot);
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnClickUse);

        if (quickslotButton != null)
            quickslotButton.onClick.RemoveListener(OnClickQuickslot);
    }

    private void OnClickUse()
    {
        if (currentItem == null || inventory == null) return;

        if (currentItem.canUseable || currentItem.canEquip)
            inventory.Consume(currentItem.itemID, 1);
    }

    private void OnClickQuickslot()
    {
        if (currentItem == null || quickSlot == null) return;
        if (!currentItem.canQuickSlot) return;

        quickSlot.TryAssignFromInventory(currentItem.itemID);
    }

    private void Start()
    {
        Hide();
    }

    private void Update()
    {
        if (tooltipRoot.activeSelf && !useFixedPosition)
        {
            UpdateMousePosition();
        }
    }

    /// <summary>
    /// 아이템 정보 표시 (슬롯 기준 고정 위치)
    /// </summary>
    public void Show(ItemBase item, RectTransform slotRect)
    {
        if (item == null) return;

        Debug.Log($"[GameInventoryToolTip] Show 호출됨! Item: {item?.itemName}");

        currentItem = item;
        currentSlotRect = slotRect;

        // 버튼 표시 설정
        bool canUse = item.canUseable;
        bool canQuick = item.canQuickSlot;

        if (useButton != null)
            useButton.gameObject.SetActive(canUse);

        if (quickslotButton != null)
            quickslotButton.gameObject.SetActive(canQuick);

        // 기본 정보
        title.text = item.itemName;
        description.text = item.description;
        category.text = GetCategoryName(item);

        if (item.tier > 0)
        {
            tier.gameObject.SetActive(true);
            tier.text = $"Tier {item.tier}";
            imgBorder.color = GetTierColor(item.tier);
        }
        else
        {
            tier.gameObject.SetActive(false);
            imgBorder.color = GetTierColor(item.tier);
        }

        // 스탯 정보 표시
        HideAllStats();

        if (item is ConsumableItemData consumable)
        {
            ShowConsumableStats(consumable);
        }
        else if (item is ProtectiveItemData protective)
        {
            ShowProtectiveStats(protective);
        }
        else if (item is ToolItemData tool)
        {
            ShowToolStats(tool);
        }
        else if (item is MiscItemData)
        {
            HideAllStats();
        }

        // 툴팁 활성화
        tooltipRoot.SetActive(true);

        // 위치 설정
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            PositionTooltipFixed();
        }
        else
        {
            if (useFixedPosition)
            {
                PositionTooltipAtSlot();
            }
        }

        ForceUpdateLayout();
    }

    /// <summary>
    /// 아이템 정보 표시 (기존 호환용 - slotRect 없이 호출 시)
    /// </summary>
    public void Show(ItemBase item)
    {
        Show(item, null);
    }

    /// <summary>
    /// 슬롯 중심 기준으로 툴팁 위치 계산 (고정 위치)
    /// </summary>
    private void PositionTooltipAtSlot()
    {
        if (currentSlotRect == null || canvasRect == null)
        {
            // slotRect가 없으면 마우스 위치 기준으로 설정
            UpdateMousePosition();
            return;
        }

        // 슬롯의 월드 위치를 캔버스 로컬 좌표로 변환
        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(
            parentCanvas.worldCamera,
            currentSlotRect.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            slotScreenPos,
            parentCanvas.worldCamera,
            out Vector2 localPos
        );

        // 오프셋 적용
        Vector2 tooltipPos = localPos + offset;

        // 화면 밖으로 나가지 않도록 클램프
        tooltipPos = ClampToScreen(tooltipPos);

        // 즉시 위치 설정 (고정이므로 Lerp 불필요)
        if (tooltipRect != null)
            tooltipRect.anchoredPosition = tooltipPos;
        else
            rectTransform.anchoredPosition = tooltipPos;
    }

    /// <summary>
    /// 화면 경계 내로 위치 제한
    /// </summary>
    private Vector2 ClampToScreen(Vector2 position)
    {
        if (canvasRect == null) return position;

        RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;
        Vector2 tooltipSize = targetRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Pivot 고려
        Vector2 pivot = targetRect.pivot;

        float minX = -canvasSize.x / 2 + tooltipSize.x * pivot.x + padding;
        float maxX = canvasSize.x / 2 - tooltipSize.x * (1 - pivot.x) - padding;
        float minY = -canvasSize.y / 2 + tooltipSize.y * pivot.y + padding;
        float maxY = canvasSize.y / 2 - tooltipSize.y * (1 - pivot.y) - padding;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    /// <summary>
    /// 마우스를 따라다니는 위치 업데이트
    /// </summary>
    private void UpdateMousePosition()
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
        RectTransform canvasRectTransform = parentCanvas.transform as RectTransform;
        Vector2 tooltipSize = rectTransform.sizeDelta;

        float minX = -canvasRectTransform.rect.width / 2;
        float maxX = canvasRectTransform.rect.width / 2 - tooltipSize.x;
        float minY = -canvasRectTransform.rect.height / 2 + tooltipSize.y;
        float maxY = canvasRectTransform.rect.height / 2;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        rectTransform.localPosition = Vector2.Lerp(
            rectTransform.localPosition,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }

    private void ForceUpdateLayout()
    {
        // TextMeshPro 텍스트 메시 강제 갱신
        title.ForceMeshUpdate();
        category.ForceMeshUpdate();
        for (int i = 0; i < statUIs.Length; i++)
        {
            statUIs[i].valueText.ForceMeshUpdate();
            statUIs[i].label.ForceMeshUpdate();
        }
        // 캔버스 갱신
        Canvas.ForceUpdateCanvases();

        // 부모부터 자식까지 순서대로 갱신
        var parentRect = title.transform.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    /// <summary>
    /// 소비 아이템 스탯 표시
    /// </summary>
    private void ShowConsumableStats(ConsumableItemData consumable)
    {
        foreach (var statUI in statUIs)
        {
            if (consumable.TryGetStat(statUI.type, out float value))
            {
                statUI.container.SetActive(true);

                string sign = "";
                if (!statUI.isNegative && value > 0)
                    sign = "+";
                else if (statUI.isNegative)
                    sign = "-";

                statUI.label.text = statUI.labelText;
                statUI.valueText.text = $"{sign}{Mathf.Abs(value)}";
            }
            else
            {
                statUI.container.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 보호구 아이템 스탯 표시
    /// </summary>
    private void ShowProtectiveStats(ProtectiveItemData protective)
    {
        foreach (var statUI in statUIs)
        {
            if (protective.TryGetStat(statUI.type, out float value))
            {
                statUI.container.SetActive(true);

                string sign = value > 0 ? "+" : "";
                statUI.label.text = statUI.labelText;
                statUI.valueText.text = $"{sign}{value}";
            }
            else
            {
                statUI.container.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 도구 아이템 스탯 표시
    /// </summary>
    private void ShowToolStats(ToolItemData tool)
    {
        foreach (var statUI in statUIs)
        {
            if (tool.TryGetStat(statUI.type, out float value))
            {
                statUI.container.SetActive(true);
                statUI.label.text = statUI.labelText;
                statUI.valueText.text = $"{value}";
            }
            else
            {
                statUI.container.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 모든 스탯 UI 숨김
    /// </summary>
    private void HideAllStats()
    {
        foreach (var statUI in statUIs)
        {
            statUI.container.SetActive(false);
        }
    }

    public void Hide()
    {
        tooltipRoot.SetActive(false);
        currentItem = null;
        currentSlotRect = null;
    }

    public static Color GetTierColor(int tier)
    {
        return tier switch
        {
            0 => Color.white,
            1 => new Color(0.7f, 0.7f, 0.7f, 150),    // 회색
            2 => new Color(0.4f, 0.7f, 1f, 150),      // 파랑
            3 => new Color(0.7f, 0.3f, 1f, 150),      // 보라
            _ => Color.white
        };
    }

    private string GetCategoryName(ItemBase item)
    {
        if (item is ConsumableItemData)
            return "소비";
        if (item is ProtectiveItemData)
            return "장비";
        if (item is ToolItemData)
            return "장비";
        if (item is MiscItemData)
            return "기타";
        return "알 수 없음";
    }

    private void PositionTooltipFixed()
    {
        RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;

        if (fixedAnchor != null)
        {
            targetRect.SetParent(fixedAnchor, worldPositionStays: false);
            targetRect.anchoredPosition = fixedAnchoredPos;
        }
        else
        {
            targetRect.SetParent(canvasRect, worldPositionStays: false);
            targetRect.anchorMin = new Vector2(1f, 0.5f);
            targetRect.anchorMax = new Vector2(1f, 0.5f);
            targetRect.pivot = new Vector2(1f, 0.5f);
            targetRect.anchoredPosition = fixedAnchoredPos;
        }
    }

}