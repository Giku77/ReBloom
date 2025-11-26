using UnityEngine;
using TMPro;

public class WeatherUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI dayCycleText;
    [SerializeField] private TextMeshProUGUI currentDayText;
    [SerializeField] private TextMeshProUGUI currentTimeText;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private StageDetector stageDetector;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    
    private float updateTimer = 0f;
    
    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }
        
        if (stageDetector == null && playerStats != null)
        {
            stageDetector = playerStats.GetComponent<StageDetector>();
        }
        
        UpdateWeatherUI();
    }
    
    private void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateWeatherUI();
        }
    }
    
    private void UpdateWeatherUI()
    {
        if (playerStats == null) return;
        
        if (dayCycleText != null)
        {
            string timeOfDay = GetCurrentDayCycle();
            dayCycleText.text = timeOfDay;
        }
        
        if (currentDayText != null)
        {
            string day = GetCurrentDay();
            currentDayText.text = day;
        }
        
        if (currentTimeText != null)
        {
            string time = GetCurrentTime();
            currentTimeText.text = time;
        }
        
        if (temperatureText != null)
        {
            string currentTemp = GetCurrentTemperature();
            temperatureText.text = currentTemp;
        }
        
        if (weatherText != null)
        {
            string weather = GetCurrentWeather();
            weatherText.text = weather;
        }
        
        if (stageText != null)
        {
            string location = GetCurrentLocation();
            stageText.text = location;
        }
    }
    
    private string GetCurrentDayCycle()
    {
        if (DayNightCycle.Instance == null)
            return "낮";

        return DayNightCycle.Instance.CurrentDayName;
    }
    
    private string GetCurrentDay()
    {
        if (DayNightCycle.Instance == null)
            return "1일차";
        
        return $"{DayNightCycle.Instance.CurrentDay}일차";
    }
    
    private string GetCurrentTime()
    {
        if (DayNightCycle.Instance == null)
            return "00시 00분";
        
        int hour = DayNightCycle.Instance.GetCurrentHour();
        int minute = DayNightCycle.Instance.GetCurrentMinute();
        
        return $"{hour:D2}시 {minute:D2}분";
    }
    private string GetCurrentWeather()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "Sunny";
        
        return stageDetector.CurrentStage.CurrentWeather.ToString();
    }
    
    private string GetCurrentLocation()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "알 수 없음";
        
        if (stageDetector.CurrentStage.Data != null)
        {
            return stageDetector.CurrentStage.Data.name;
        }
        
        return "알 수 없음";
    }
    private string GetCurrentTemperature()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "36.5°C";

        if (stageDetector.CurrentStage.Data != null)
        {
            return $"{stageDetector.GetCurrentTemperatureMultiplier():F1}°C";
        }

        return "36.5°C";
    }
}