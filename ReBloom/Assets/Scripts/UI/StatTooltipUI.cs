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
        float totalHpLoss = 0f;

        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 1)
                {
                    state = debuff.Name;
                    totalHpLoss += debuff.HpLoss;
                    break;
                }
            }
        }

        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }

        if (playerStats.Pollution is PollutionStat pollutionStat)
        {
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: {totalHpLoss:F1}";
            }
            // 오염도는 속도 감소 없음 - 숨기거나 비활성화
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.gameObject.SetActive(false);
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
        float totalHpLoss = 0f;
        float totalSpeedReduce = 0f;

        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 2)
                {
                    state = debuff.Name;
                    totalHpLoss += debuff.HpLoss;
                    totalSpeedReduce += debuff.SpeedReduce;
                    break;
                }
            }
        }

        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }

        if (playerStats.Thirst is ThirstStat thirstStat)
        {
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: {totalHpLoss:F1}";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.gameObject.SetActive(true);
                // ★ 수정: totalSpeedReduce를 %로 변환
                int speedPercent = Mathf.RoundToInt(totalSpeedReduce * 100f);
                speedDecreaseRateText.text = $"이동 속도 감소량: {speedPercent}%";
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
            statValueText.text = $"현재 수치: {hungerPercent}%";
        }

        string state = "정상";
        float totalHpLoss = 0f;
        float totalSpeedReduce = 0f;

        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                if (debuff.Category == 3)
                {
                    state = debuff.Name;
                    totalHpLoss += debuff.HpLoss;
                    totalSpeedReduce += debuff.SpeedReduce;
                    break;
                }
            }
        }

        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }

        if (playerStats.Hunger is HungerStat hungerStat)
        {
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: {totalHpLoss:F1}";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.gameObject.SetActive(true);
                int speedPercent = Mathf.RoundToInt(totalSpeedReduce * 100f);
                speedDecreaseRateText.text = $"이동 속도 감소량: {speedPercent}%";
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
        float totalHpLoss = 0f;
        float totalSpeedReduce = 0f;

        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                // 체온 관련: Category 4(저체온), 5(중증저체온), 6(고열), 7(열사병)
                if (debuff.Category >= 4 && debuff.Category <= 7)
                {
                    state = debuff.Name;
                    totalHpLoss += debuff.HpLoss;
                    totalSpeedReduce += debuff.SpeedReduce;
                    break;
                }
            }
        }

        if (stateText != null)
        {
            stateText.text = $"상태 : {state}";
        }

        if (playerStats.Temperature is TemperatureStat temperatureStat)
        {
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: {totalHpLoss:F1}";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.gameObject.SetActive(true);

                int speedPercent = Mathf.RoundToInt(totalSpeedReduce * 100f);
                speedDecreaseRateText.text = $"이동 속도 감소량: {speedPercent}%";
            }
            descriptionText.text = "34°C 이하: 저체온증 / 31°C 이하: 중증 저체온 / 38°C 이상: 고열 / 41°C 이상: 열사병";
            stateBorder.color = GetStateColor(StatType.Temperature);
        }
    }
    private void UpdateHPTooltip()
    {
        statTitle.text = "체력";
        float hpValue = playerStats.Health.Value;
        float hpMax = playerStats.Health.MaxValue;

        if (statValueText != null)
        {
            statValueText.text = $"현재 수치 : {hpValue:F0} / {hpMax:F0}";
        }

        // ★ 모든 활성 디버프의 영향 합산
        float totalHpLoss = 0f;
        float totalSpeedReduce = 0f;

        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            foreach (var debuff in activeDebuffs)
            {
                totalHpLoss += debuff.HpLoss;
                totalSpeedReduce += debuff.SpeedReduce;
            }
        }

        if (playerStats.Health is HealthStat health)
        {
            if (hpDecreaseRateText != null)
            {
                hpDecreaseRateText.text = $"초당 체력 감소량: {totalHpLoss:F1}";
            }
            if (speedDecreaseRateText != null)
            {
                speedDecreaseRateText.gameObject.SetActive(true);
                int speedPercent = Mathf.RoundToInt(totalSpeedReduce * 100f);
                speedDecreaseRateText.text = $"이동 속도 감소량: {speedPercent}%";
            }
            descriptionText.text = "체력이 0이 되면 사망합니다.";
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