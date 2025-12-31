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
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI stats;
    [SerializeField] private TextMeshProUGUI category;
    [SerializeField] private TextMeshProUGUI tier;
    [SerializeField] private Image imgBorder;
    [SerializeField] private Button useButton;
    [SerializeField] private Button quickslotButton;

    private QuickSlot quickSlot;
    private GameInventory inventory;

    public Button UseButton => useButton;
    public Button QuickslotButton => quickslotButton;

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
        if (tooltipRoot.activeSelf)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 아이템 정보 표시
    /// </summary>
    public void Show(ItemBase item)
    {
        Debug.Log($"[GameInventoryToolTip] Show 호출됨! Item: {item?.itemName}");
        currentItem = item;

        bool canUse = item != null && item.canUseable;
        bool canQuick = item != null && item.canQuickSlot;

        if (useButton != null)
        {
            useButton.gameObject.SetActive(canUse);
        }

        if (quickslotButton != null)
        {
            quickslotButton.gameObject.SetActive(canQuick);
        }

        // 기본 정보
        title.text = item.itemName;
        description.text = item.description;
        category.text = GetCategoryName(item);

        if (item.tier > 0)
        {
            tier.gameObject.SetActive(true);
            tier.text = $"Tier {item.tier}";

            // 티어 색상
            imgBorder.color = GetTierColor(item.tier);
        }
        else
        {
            tier.gameObject.SetActive(false);
            imgBorder.color = GetTierColor(item.tier);
        }


        HideAllStats();
        // 스탯 정보 (아이템 타입별 분기)
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
        tooltipRoot.SetActive(true);
        ForceUpdateLayout();
    }
    private void ForceUpdateLayout()
    {
        // TextMeshPro 텍스트 메시 강제 갱신
        title.ForceMeshUpdate();
        category.ForceMeshUpdate();
        for (int i = 0; i < statUIs.Length; i++)
        {
            statUIs[i].valueText.ForceMeshUpdate();  // 값 표시용
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

    public static Color GetTierColor(int tier)
    {
        return tier switch
        {
            0 => Color.white,                     //
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
}