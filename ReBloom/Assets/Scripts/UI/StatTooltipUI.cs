using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatTooltipUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI statTitle;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI hpDecreaseRateText;
    [SerializeField] private TextMeshProUGUI speedDecreaseRateText;
    [SerializeField] private Image stateBorder;
    //[SerializeField] private TextMeshProUGUI statRateText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Stat Type")]
    [SerializeField] private StatType statType = StatType.Pollution;
    [SerializeField] private Color stateColor;
    
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
            case StatType.HP:
                UpdateHPTooltip();
                break;
        }
    }
    
    private void UpdatePollutionTooltip()
    {
        statTitle.text = "오염도";
        float pollutionValue = playerStats.Pollution.Value;
        float pollutionMax = playerStats.Pollution.MaxValue;
        int pollutionPercent = Mathf.RoundToInt((pollutionValue / pollutionMax) * 100f);
        
        if (statValueText != null)
        {
            statValueText.text = $"현재 수치: {pollutionPercent}%";
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
        
        //float actualRate = 0f;
        
        if (playerStats.Pollution is PollutionStat pollutionStat)
        {
            //actualRate = pollutionStat.ActualRate;
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량 : ??";
            }
            descriptionText.text = "오염도 수치가 100%에 도달하면 중독 상태에 빠져 체력이 감소합니다.";
            stateBorder.color = GetStateColor(StatType.Pollution);
        }
    }
    
    private void UpdateThirstTooltip()
    {
        statTitle.text = "갈증";
        float thirstValue = playerStats.Thirst.Value;
        float thirstMax = playerStats.Thirst.MaxValue;
        int thirstPercent = Mathf.RoundToInt((thirstValue / thirstMax) * 100f);
        
        if (statValueText != null)
        {
            statValueText.text = $"현재 수치: {thirstPercent}%";
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
        
        //float actualRate = 0f;
        
        if (playerStats.Thirst is ThirstStat thirstStat)
        {
           // actualRate = thirstStat.ActualRate;
           //actualRate:F3;
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: ??";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.text = $"이동 속도 감소량: ??";
            }
            descriptionText.text = "갈증 수치가 30% / 60% / 100% 에 도달하면 상태 이상 단계가 상승하고 디버프가 적용됩니다.";
            stateBorder.color = GetStateColor(StatType.Thirst);
        }
    }
    
    private void UpdateHungerTooltip()
    {
        statTitle.text = "허기";
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
        
        //float actualRate = 0f;
        
        if (playerStats.Hunger is HungerStat hungerStat)
        {
            //actualRate = hungerStat.ActualRate;
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: ??";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.text = $"이동 속도 감소량: ??";
            }
            descriptionText.text = "허기 수치가 30% / 60% / 100% 에 도달하면 상태 이상 단계가 상승하고 디버프가 적용됩니다.";
            stateBorder.color = GetStateColor(StatType.Hunger);
        }
    }

    private void UpdateTemperatureTooltip()
    {
        statTitle.text = "체온";
        float tempValue = playerStats.Temperature.Value;

        if (statValueText != null)
        {
            statValueText.text = $"현재 수치 : {tempValue:F1}°C";
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

        //float actualRate = 0f;

        if (playerStats.Temperature is TemperatureStat temperatureStat)
        {
           // actualRate = temperatureStat.ActualRate;
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: ??";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.text = $"이동 속도 감소량: ??";
            }
            descriptionText.text = "34°C 이하: 저체온증 / 31°C 이하: 중증 저체온 / 38°C 이상: 고열 / 41°C 이상: 열사병";
            stateBorder.color = GetStateColor(StatType.Temperature);
        }
    }
    private void UpdateHPTooltip()
    {
        statTitle.text = "체력";
        float tempValue = playerStats.Temperature.Value;

        if (statValueText != null)
        {
            statValueText.text = $"현재 수치 : {tempValue:F1}°C";
        }

        //float actualRate = 0f;

        if (playerStats.Health is HealthStat health)
        {
           // actualRate = temperatureStat.ActualRate;
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: ??";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.text = $"이동 속도 감소량: ??";
            }
            descriptionText.text = " ";
            stateBorder.color = GetStateColor(StatType.HP);
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

    private Color GetStateColor(StatType statType)
    {
        Color color = Color.white; // Default color

        switch (statType)
        {
            case StatType.Pollution:
                color = stateColor;
                break;
            case StatType.Hunger:
                color = stateColor;
                break;
            case StatType.Thirst:
                color = stateColor;
                break;
            case StatType.HP:
                color = stateColor;
                break;
            case StatType.Temperature:
                color = stateColor;
                break;
        }
        return color;
    }
}