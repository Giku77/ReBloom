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

    [Header("PC Position Settings")]
    [SerializeField] private Vector2 pcOffset = new Vector2(120f, 0f);
    [SerializeField] private float padding = 10f;
    [SerializeField] private bool followMouse = false;  // true: 마우스 따라다님, false: 슬롯 기준 고정
    [SerializeField] private float followSpeed = 10f;

    [Header("Mobile Position Settings")]
    [SerializeField] private Vector2 mobileAnchoredPos = new Vector2(-30f, 0f);
    [SerializeField] private RectTransform mobileFixedAnchor;  // 모바일 고정 위치 앵커

    private QuickSlot quickSlot;
    private GameInventory inventory;

    public Button UseButton => useButton;
    public Button QuickslotButton => quickslotButton;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private ItemBase currentItem;
    private RectTransform currentSlotRect;  // 현재 호버 중인 슬롯

    private bool isMobile;
    private Vector2 cachedSlotPosition;  // 슬롯 위치 캐싱

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

    private void Start()
    {
        Hide();
    }
    private void Update()
    {
        // PC - 마우스 따라다니기 모드일 때만
        if (tooltipRoot.activeSelf && !isMobile && followMouse)
        {
            UpdateMousePosition();
        }
    }

    /// <summary>
    /// 아이템 정보 표시 (슬롯 기준 고정 위치)
    /// </summary>
    /// 

    public void Show(ItemBase item, RectTransform slotRect)
    {
        if (item == null) return;

        currentItem = item;
        currentSlotRect = slotRect;

        // 플랫폼 체크
        isMobile = PlatformManager.Instance != null && PlatformManager.Instance.IsMobile;

        // 버튼 설정
        SetupButtons(item);

        // 텍스트 설정
        SetupTexts(item);

        // 스탯 설정
        SetupStats(item);

        // 툴팁 활성화
        tooltipRoot.SetActive(true);

        // 플랫폼별 위치 설정
        if (isMobile)
        {
            PositionForMobile();
        }
        else
        {
            PositionForPC(slotRect);
        }

        ForceUpdateLayout();
    }

    public void Show(ItemBase item)
    {
        Show(item, null);
    }

    #region 위치 설정 (PC/Mobile 분리)

    /// <summary>
    /// PC용 위치 설정
    /// </summary>
    private void PositionForPC(RectTransform slotRect)
    {
        RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;

        if (slotRect == null || followMouse)
        {
            // 마우스 위치 기준
            UpdateMousePosition();
            return;
        }

        // 슬롯 위치를 캔버스 로컬 좌표로 변환
        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(
            parentCanvas.worldCamera,
            slotRect.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            slotScreenPos,
            parentCanvas.worldCamera,
            out Vector2 localPos
        );

        // 오프셋 적용 + 화면 경계 클램프
        Vector2 tooltipPos = ClampToScreen(localPos + pcOffset);
        targetRect.anchoredPosition = tooltipPos;

        // 위치 캐싱 (슬롯이 움직여도 고정)
        cachedSlotPosition = tooltipPos;
    }

    /// <summary>
    /// 모바일용 위치 설정 (고정 위치)
    /// </summary>
    private void PositionForMobile()
    {
        RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;

        if (mobileFixedAnchor != null)
        {
            // 앵커 기준 위치 계산 (부모 변경 없이!)
            Vector2 anchorScreenPos = RectTransformUtility.WorldToScreenPoint(
                parentCanvas.worldCamera,
                mobileFixedAnchor.position
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                anchorScreenPos,
                parentCanvas.worldCamera,
                out Vector2 localPos
            );

            targetRect.anchoredPosition = localPos + mobileAnchoredPos;
        }
        else
        {
            // 앵커 없으면 화면 오른쪽 중앙에 고정
            float rightEdge = canvasRect.rect.width / 2 - padding;
            targetRect.anchoredPosition = new Vector2(
                rightEdge + mobileAnchoredPos.x,
                mobileAnchoredPos.y
            );
        }
    }
    #endregion

    //public void Show(ItemBase item, RectTransform slotRect)
    //{
    //    if (item == null) return;

    //    Debug.Log($"[GameInventoryToolTip] Show 호출됨! Item: {item?.itemName}");

    //    currentItem = item;
    //    currentSlotRect = slotRect;

    //    // 버튼 표시 설정
    //    bool canUse = item.canUseable;
    //    bool canQuick = item.canQuickSlot;

    //    if (useButton != null)
    //        useButton.gameObject.SetActive(canUse);

    //    if (quickslotButton != null)
    //        quickslotButton.gameObject.SetActive(canQuick);

    //    // 기본 정보
    //    title.text = item.itemName;
    //    description.text = item.description;
    //    category.text = GetCategoryName(item);

    //    if (item.tier > 0)
    //    {
    //        tier.gameObject.SetActive(true);
    //        tier.text = $"Tier {item.tier}";
    //        imgBorder.color = GetTierColor(item.tier);
    //    }
    //    else
    //    {
    //        tier.gameObject.SetActive(false);
    //        imgBorder.color = GetTierColor(item.tier);
    //    }

    //    // 스탯 정보 표시
    //    HideAllStats();

    //    if (item is ConsumableItemData consumable)
    //    {
    //        ShowConsumableStats(consumable);
    //    }
    //    else if (item is ProtectiveItemData protective)
    //    {
    //        ShowProtectiveStats(protective);
    //    }
    //    else if (item is ToolItemData tool)
    //    {
    //        ShowToolStats(tool);
    //    }
    //    else if (item is MiscItemData)
    //    {
    //        HideAllStats();
    //    }

    //    // 툴팁 활성화
    //    tooltipRoot.SetActive(true);

    //    // 위치 설정
    //    if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
    //    {
    //        PositionTooltipFixed();
    //    }
    //    else
    //    {
    //        if (useFixedPosition)
    //        {
    //            PositionTooltipAtSlot();
    //        }
    //    }

    //    ForceUpdateLayout();
    //}

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
    /// 마우스 따라다니기 (PC 전용)
    /// </summary>
    private void UpdateMousePosition()
    {
        if (parentCanvas == null) return;

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePos,
            parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 targetPos = ClampToScreen(localPoint + pcOffset);

        RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;
        targetRect.anchoredPosition = Vector2.Lerp(
            targetRect.anchoredPosition,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }

    ///// <summary>
    ///// 화면 경계 클램프
    ///// </summary>
    //private Vector2 ClampToScreen(Vector2 position)
    //{
    //    if (canvasRect == null) return position;

    //    RectTransform targetRect = tooltipRect != null ? tooltipRect : rectTransform;
    //    Vector2 tooltipSize = targetRect.sizeDelta;
    //    Vector2 canvasSize = canvasRect.sizeDelta;
    //    Vector2 pivot = targetRect.pivot;

    //    float minX = -canvasSize.x / 2 + tooltipSize.x * pivot.x + padding;
    //    float maxX = canvasSize.x / 2 - tooltipSize.x * (1 - pivot.x) - padding;
    //    float minY = -canvasSize.y / 2 + tooltipSize.y * pivot.y + padding;
    //    float maxY = canvasSize.y / 2 - tooltipSize.y * (1 - pivot.y) - padding;

    //    position.x = Mathf.Clamp(position.x, minX, maxX);
    //    position.y = Mathf.Clamp(position.y, minY, maxY);

    //    return position;
    //}

    #region 설정 헬퍼

    private void SetupButtons(ItemBase item)
    {
        if (useButton != null)
            useButton.gameObject.SetActive(item.canUseable);

        if (quickslotButton != null)
            quickslotButton.gameObject.SetActive(item.canQuickSlot);
    }

    private void SetupTexts(ItemBase item)
    {
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
    }

    private void SetupStats(ItemBase item)
    {
        HideAllStats();

        if (item is ConsumableItemData consumable)
            ShowConsumableStats(consumable);
        else if (item is ProtectiveItemData protective)
            ShowProtectiveStats(protective);
        else if (item is ToolItemData tool)
            ShowToolStats(tool);
    }

    #endregion

    #region 버튼 이벤트

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

    #endregion

    #region 스탯 표시

    private void ShowConsumableStats(ConsumableItemData consumable)
    {
        foreach (var statUI in statUIs)
        {
            if (consumable.TryGetStat(statUI.type, out float value))
            {
                statUI.container.SetActive(true);
                string sign = !statUI.isNegative && value > 0 ? "+" : (statUI.isNegative ? "-" : "");
                statUI.label.text = statUI.labelText;
                statUI.valueText.text = $"{sign}{Mathf.Abs(value)}";
            }
            else
            {
                statUI.container.SetActive(false);
            }
        }
    }

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

    private void HideAllStats()
    {
        foreach (var statUI in statUIs)
        {
            statUI.container.SetActive(false);
        }
    }

    #endregion

    #region 유틸리티

    public void Hide()
    {
        tooltipRoot.SetActive(false);
        currentItem = null;
        currentSlotRect = null;
    }

    private void ForceUpdateLayout()
    {
        title.ForceMeshUpdate();
        category.ForceMeshUpdate();

        foreach (var statUI in statUIs)
        {
            statUI.valueText.ForceMeshUpdate();
            statUI.label.ForceMeshUpdate();
        }

        Canvas.ForceUpdateCanvases();

        var parentRect = title.transform.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    public static Color GetTierColor(int tier)
    {
        return tier switch
        {
            0 => Color.white,
            1 => new Color(0.7f, 0.7f, 0.7f, 150),
            2 => new Color(0.4f, 0.7f, 1f, 150),
            3 => new Color(0.7f, 0.3f, 1f, 150),
            _ => Color.white
        };
    }

    private string GetCategoryName(ItemBase item)
    {
        if (item is ConsumableItemData) return "소비";
        if (item is ProtectiveItemData) return "장비";
        if (item is ToolItemData) return "장비";
        if (item is MiscItemData) return "기타";
        return "알 수 없음";
    }

    #endregion
}