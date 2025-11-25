using UnityEngine;
using TMPro;

public class WeatherUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private TextMeshProUGUI StageText;
    
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
        
        if (StageText != null)
        {
            string location = GetCurrentLocation();
            StageText.text = location;
        }
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