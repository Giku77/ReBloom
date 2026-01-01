using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherUI : UIBase
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI dayCycleText;
    [SerializeField] private TextMeshProUGUI currentDayText;
    [SerializeField] private TextMeshProUGUI currentTimeText;

    [Header("weather UI Elements")]
    [SerializeField] private Image weatherBackground;     // 날씨 배경 패널

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private StageDetector stageDetector;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    
    private float updateTimer = 0f;

    // 날씨별 색상 딕셔너리
    private static readonly Dictionary<WeatherType, Color> WeatherColors = new Dictionary<WeatherType, Color>
    {
        { WeatherType.Sunny,      HexToColor("#FFD700") },  // 맑음 - 골드 톤
        { WeatherType.Rain,       HexToColor("#4A90E2") },  // 비 - 맑은 파랑
        { WeatherType.Radio,    HexToColor("#899E7F") },  // 방사능 낙진 - 탁한 연두
        { WeatherType.Snow,       HexToColor("#BEE6FA") },  // 눈 - 연한 하늘색
        { WeatherType.Thunder,    HexToColor("#8E44AD") },  // 천둥번개 - 어두운 보라
        { WeatherType.Hot,   HexToColor("#FF6400") },  // 폭염 - 진한 주황
    };

    // 날씨별 한글 이름 딕셔너리
    private static readonly Dictionary<WeatherType, string> WeatherNames = new Dictionary<WeatherType, string>
    {
        { WeatherType.Sunny,   "맑음" },
        { WeatherType.Rain,    "비" },
        { WeatherType.Radio,   "방사능 낙진" },
        { WeatherType.Snow,    "눈" },
        { WeatherType.Thunder, "천둥번개" },
        { WeatherType.Hot,     "폭염" },
    };

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
            WeatherType currentWeather = GetCurrentWeatherType();
            weatherText.text = GetWeatherName(currentWeather);
        }
        
        if (stageText != null)
        {
            string location = GetCurrentLocation();
            stageText.text = location;
        }

        // 날씨 아이콘/배경 색상 적용 (선택사항)
        WeatherType weatherType = GetCurrentWeatherType();

        if (weatherBackground != null)
        {
            Color bgColor = GetWeatherColor(weatherType);
            bgColor.a = 0.6f;
            weatherBackground.color = bgColor;
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
        //if (DayNightCycle.Instance == null)
        //    return "00시 00분";

        //int hour = DayNightCycle.Instance.GetCurrentHour();
        //int minute = DayNightCycle.Instance.GetCurrentMinute();

        //return $"{hour:D2}시 {minute:D2}분";

        if (DayNightCycle.Instance == null)
            return "12:00 AM";

        int hour = DayNightCycle.Instance.GetCurrentHour();
        int minute = DayNightCycle.Instance.GetCurrentMinute();

        // 24시간 -> 12시간 AM/PM 변환
        string period = hour >= 12 ? "PM" : "AM";
        int hour12 = hour % 12;
        if (hour12 == 0) hour12 = 12;  // 0시 -> 12 AM, 12시 -> 12 PM

        return $"{hour12:D2}:{minute:D2} {period}";
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

    /// <summary>
    /// 현재 날씨 타입 가져오기
    /// </summary>
    private WeatherType GetCurrentWeatherType()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return WeatherType.Sunny;

        return stageDetector.CurrentStage.CurrentWeather;
    }

    //private string GetCurrentWeather()
    //{
    //    if (stageDetector == null || stageDetector.CurrentStage == null)
    //        return "Sunny";

    //    return stageDetector.CurrentStage.CurrentWeather.ToString();
    //}

    /// <summary>
    /// 날씨 타입에 해당하는 한글 이름 반환 GetCurrentWeather() 대체
    /// </summary>
    private static string GetWeatherName(WeatherType weatherType)
    {
        if (WeatherNames.TryGetValue(weatherType, out string name))
            return name;

        return weatherType.ToString();
    }

    /// <summary>
    /// HEX 색상 코드를 Color로 변환
    /// </summary>
    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;

        return Color.white;
    }

    /// <summary>
    /// 날씨 타입에 해당하는 색상 반환
    /// </summary>
    public static Color GetWeatherColor(WeatherType weatherType)
    {
        if (WeatherColors.TryGetValue(weatherType, out Color color))
            return color;

        return Color.white;  // 기본값
    }
}