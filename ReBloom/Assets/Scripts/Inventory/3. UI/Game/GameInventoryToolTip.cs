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
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI label;
    public string labelText;
    public bool isNegative;
}

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

    [Header("PC Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, -10);
    [SerializeField] private float followSpeed = 10f;

    [Header("Mobile Settings")]
    [SerializeField] private RectTransform mobileAnchorPoint;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private ItemBase currentItem;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        Hide();
    }

    private void Update()
    {
        if (tooltipRoot.activeSelf && PlatformManager.Instance.IsPC)
        {
            UpdateMousePosition();
        }
    }

    public void Show(ItemBase item)
    {
        Debug.Log($"[GameInventoryToolTip] Show 호출됨! Item: {item?.itemName}");
        currentItem = item;

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

        HideAllStats();

        if (item is ConsumableItemData consumable)
            ShowConsumableStats(consumable);
        else if (item is ProtectiveItemData protective)
            ShowProtectiveStats(protective);
        else if (item is ToolItemData tool)
            ShowToolStats(tool);

        tooltipRoot.SetActive(true);
        ForceUpdateLayout();

        // 모바일이면 고정 위치로 설정
        if (PlatformManager.Instance.IsMobile)
        {
            SetMobilePosition();
        }
    }

    private void SetMobilePosition()
    {
        if (mobileAnchorPoint == null)
        {
            Debug.LogWarning("[GameInventoryToolTip] mobileAnchorPoint가 설정되지 않음!");
            return;
        }

        rectTransform.position = mobileAnchorPoint.position;
    }

    private void UpdateMousePosition()
    {
        if (parentCanvas == null) return;

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePos,
            parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 targetPos = localPoint + offset;

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

    // === 나머지 메서드들 (변경 없음) ===

    private void ForceUpdateLayout()
    {
        title.ForceMeshUpdate();
        category.ForceMeshUpdate();
        for (int i = 0; i < statUIs.Length; i++)
        {
            statUIs[i].valueText.ForceMeshUpdate();
            statUIs[i].label.ForceMeshUpdate();
        }
        Canvas.ForceUpdateCanvases();

        var parentRect = title.transform.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

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

    public void Hide()
    {
        tooltipRoot.SetActive(false);
        currentItem = null;
    }

    public static Color GetTierColor(int tier)
    {
        return tier switch
        {
            0 => Color.white,
            1 => new Color(0.7f, 0.7f, 0.7f),
            2 => new Color(0.3f, 0.6f, 1f),
            3 => new Color(0.8f, 0.3f, 1f),
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
}