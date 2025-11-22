using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatTooltipUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI statRateText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Stat Type")]
    [SerializeField] private StatType statType = StatType.Pollution;
    
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private DebuffManager debuffManager;
    
    [Header("Position Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10f, 10f);
    
    private RectTransform tooltipRect;
    private Canvas canvas;

    private void Awake()
    {
        if (tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            tooltipPanel.SetActive(false);
            
            DisableRaycastTarget(tooltipPanel);
        }
    }
    
    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }
        
        if (debuffManager == null && playerStats != null)
        {
            debuffManager = playerStats.GetComponent<DebuffManager>();
        }
    }
    
    public void ShowTooltip(Vector2 mousePosition)
    {
        if (tooltipPanel == null || playerStats == null) return;
        
        UpdateTooltipContent();
        tooltipPanel.SetActive(true);
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePosition,
            canvas.worldCamera,
            out localPoint
        );
        
        tooltipRect.anchoredPosition = localPoint + offset;
    }
    
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    private void UpdateTooltipContent()
    {
        if (playerStats == null) return;
        
        switch (statType)
        {
            case StatType.Pollution:
                UpdatePollutionTooltip();
                break;
            case StatType.Thirst:
                UpdateThirstTooltip();
                break;
            case StatType.Hunger:
                UpdateHungerTooltip();
                break;
            case StatType.Temperature:
                UpdateTemperatureTooltip();
                break;
        }
    }
    
    private void UpdatePollutionTooltip()
    {
        float pollutionValue = playerStats.Pollution.Value;
        float pollutionMax = playerStats.Pollution.MaxValue;
        int pollutionPercent = Mathf.RoundToInt((pollutionValue / pollutionMax) * 100f);
        
        if (statValueText != null)
        {
            statValueText.text = $"오염도 : {pollutionPercent}%";
        }
        
        string state = "정상";
        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 1)
                {
                    state = debuff.Name;
                    break;
                }
            }
        }
        
        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }
        
        float actualRate = 0f;
        
        if (playerStats.Pollution is PollutionStat pollutionStat)
        {
            actualRate = pollutionStat.ActualRate;
            if (statRateText != null)
            {
                statRateText.text = $"초당 증가량 : {actualRate:F3}";
            }
        }
    }
    
    private void UpdateThirstTooltip()
    {
        float thirstValue = playerStats.Thirst.Value;
        float thirstMax = playerStats.Thirst.MaxValue;
        int thirstPercent = Mathf.RoundToInt((thirstValue / thirstMax) * 100f);
        
        if (statValueText != null)
        {
            statValueText.text = $"갈증 : {thirstPercent}%";
        }
        
        string state = "정상";
        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 2)
                {
                    state = debuff.Name;
                    break;
                }
            }
        }
        
        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }
        
        float actualRate = 0f;
        
        if (playerStats.Thirst is ThirstStat thirstStat)
        {
            actualRate = thirstStat.ActualRate;
            if (statRateText != null)
            {
                statRateText.text = $"초당 증가량 : {actualRate:F3}";
            }
        }
    }
    
    private void UpdateHungerTooltip()
    {
        float hungerValue = playerStats.Hunger.Value;
        float hungerMax = playerStats.Hunger.MaxValue;
        int hungerPercent = Mathf.RoundToInt((hungerValue / hungerMax) * 100f);
        
        if (statValueText != null)
        {
            statValueText.text = $"허기 : {hungerPercent}%";
        }
        
        string state = "정상";
        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 3)
                {
                    state = debuff.Name;
                    break;
                }
            }
        }
        
        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }
        
        float actualRate = 0f;
        
        if (playerStats.Hunger is HungerStat hungerStat)
        {
            actualRate = hungerStat.ActualRate;
            if (statRateText != null)
            {
                statRateText.text = $"초당 증가량 : {actualRate:F3}";
            }
        }
    }

    private void UpdateTemperatureTooltip()
    {
        float tempValue = playerStats.Temperature.Value;

        if (statValueText != null)
        {
            statValueText.text = $"체온 : {tempValue:F1}%";
        }

        string state = "정상";
        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 4)
                {
                    state = debuff.Name;
                    break;
                }
            }
        }

        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }

        float actualRate = 0f;

        if (playerStats.Temperature is TemperatureStat temperatureStat)
        {
            actualRate = temperatureStat.ActualRate;
            if (statRateText != null)
            {
                statRateText.text = $"초당 증가량 : {actualRate:F3}";
            }
        }
    }

    private void DisableRaycastTarget(GameObject obj)
    {
        var graphics = obj.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }
}