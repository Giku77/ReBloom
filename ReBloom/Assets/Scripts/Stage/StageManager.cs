using UnityEngine;
using System.Collections.Generic;
using System;


public class StageManager : MonoBehaviour
{ 
    private StageDB stageDB;
    private Dictionary<int, WeatherInfo> weatherByStageID = new Dictionary<int, WeatherInfo>();

    public static event Action<int, WeatherType> OnWeatherChange;
    
    public StageDB DB => stageDB;
    
    private void Awake()
    {    
        stageDB = new StageDB();
        stageDB.LoadFromBG();
        
        // 각 스테이지별 날씨 정보 초기화
        foreach (var kvp in stageDB.GetAll())
        {
            weatherByStageID[kvp.Key] = new WeatherInfo();
        }
    }
    
    private void Start()
    {
        InitializeAllStages();
    }
    
    private void InitializeAllStages()
    {
        //StageBase[] stages = FindObjectsOfType<StageBase>();
        StageBase[] stages = FindObjectsByType<StageBase>(FindObjectsSortMode.None);

        foreach (var stage in stages)
        {
            stage.Initialize(stageDB);
        }
    }

    public bool TryGetStageData(int id, out StageData data)
    {
        return stageDB.TryGet(id, out data);
    }


    public void SetWeatherRaw(
    int stageID,
    WeatherType weather,
    float weatherTimer,
    float weatherDuration,
    float pollution,
    float thirst,
    float temp,
    bool raiseEvent = true)
    {
        if (!weatherByStageID.ContainsKey(stageID))
            weatherByStageID[stageID] = new WeatherInfo();

        var info = weatherByStageID[stageID];

        info.currentWeather = weather;
        info.weatherTimer = Mathf.Max(0f, weatherTimer);
        info.weatherDuration = Mathf.Max(1f, weatherDuration);

        info.currentPollution = pollution;
        info.currentThirst = thirst;
        info.currentTemp = temp;

        if (raiseEvent)
            OnWeatherChange?.Invoke(stageID, weather);
    }

    
    private void SetRandomWeather(int stageID)
    {
        if (!stageDB.TryGet(stageID, out StageData data)) return;
        if (!weatherByStageID.ContainsKey(stageID)) return;
        
        float totalRate = data.sunnyRate + data.rainRate + data.radioRate + 
                         data.snowRate + data.thunderRate + data.hotRate;
        
        float random = UnityEngine.Random.Range(0f, totalRate);
        float accumulated = 0f;
        
        accumulated += data.sunnyRate;
        if (random < accumulated)
        {
            SetWeather(stageID, WeatherType.Sunny);
            return;
        }
        
        accumulated += data.rainRate;
        if (random < accumulated)
        {
            SetWeather(stageID, WeatherType.Rain);
            return;
        }
        
        accumulated += data.radioRate;
        if (random < accumulated)
        {
            SetWeather(stageID, WeatherType.Radio);
            return;
        }
        
        accumulated += data.snowRate;
        if (random < accumulated)
        {
            SetWeather(stageID, WeatherType.Snow);
            return;
        }
        
        accumulated += data.thunderRate;
        if (random < accumulated)
        {
            SetWeather(stageID, WeatherType.Thunder);
            return;
        }
        
        SetWeather(stageID, WeatherType.Hot);
    }
    
    public void SetWeather(int stageID, WeatherType weather)
    {
        if (!stageDB.TryGet(stageID, out StageData data)) return;
        if (!weatherByStageID.ContainsKey(stageID)) return;
        
        WeatherInfo info = weatherByStageID[stageID];
        info.currentWeather = weather;
        info.weatherTimer = 0f;
        
        switch (weather)
        {
            case WeatherType.Sunny:
                info.weatherDuration = data.sunny_d + UnityEngine.Random.Range(-data.sunny_vari, data.sunny_vari);
                info.currentPollution = data.sunnyPollution;
                info.currentThirst = data.sunnyThirst;
                info.currentTemp = data.sunnyTemp;
                break;
                
            case WeatherType.Rain:
                info.weatherDuration = data.rain_d + UnityEngine.Random.Range(-data.rain_vari, data.rain_vari);
                info.currentPollution = data.rainPollution;
                info.currentThirst = data.rainThirst;
                info.currentTemp = data.rainTemp;
                break;
                
            case WeatherType.Radio:
                info.weatherDuration = data.radio_d + UnityEngine.Random.Range(-data.radio_vari, data.radio_vari);
                info.currentPollution = data.radioPollution;
                info.currentThirst = data.radioThirst;
                info.currentTemp = data.radioTemp;
                break;
                
            case WeatherType.Snow:
                info.weatherDuration = data.snow_d + UnityEngine.Random.Range(-data.snow_vari, data.snow_vari);
                info.currentPollution = data.snowPollution;
                info.currentThirst = data.snowThirst;
                info.currentTemp = data.snowTemp;
                break;
                
            case WeatherType.Thunder:
                info.weatherDuration = data.thunde_d + UnityEngine.Random.Range(-data.thunde_vari, data.thunde_vari);
                info.currentPollution = data.thundePollution;
                info.currentThirst = data.thundeThirst;
                info.currentTemp = data.thundeTemp;
                break;
                
            case WeatherType.Hot:
                info.weatherDuration = data.hot_d + UnityEngine.Random.Range(-data.hot_vari, data.hot_vari);
                info.currentPollution = data.hotPollution;
                info.currentThirst = data.hotThirst;
                info.currentTemp = data.hotTemp;
                break;
        }

        OnWeatherChange?.Invoke(stageID, weather);
        //Debug.Log($"[StageManager] Stage {data.name} 날씨 변경: {weather} (지속시간: {info.weatherDuration:F1}초, 오염도: {info.currentPollution}, 갈증: {info.currentThirst}, 온도: {info.currentTemp})");
    }
    
    public WeatherInfo GetWeatherInfo(int stageID)
    {
        if (weatherByStageID.ContainsKey(stageID))
        {
            return weatherByStageID[stageID];
        }
        return null;
    }
}
