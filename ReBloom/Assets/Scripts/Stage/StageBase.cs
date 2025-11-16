using UnityEngine;

public class StageBase : MonoBehaviour
{
    [Header("Stage Data")]
    public int stageID;
    
    private StageData stageData;
    private StageManager stageManager;
    
    public int StageID => stageID;
    public StageData Data => stageData;
    
    public WeatherType CurrentWeather
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.currentWeather ?? WeatherType.Sunny;
        }
    }
    
    public float CurrentPollution
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.currentPollution ?? 0f;
        }
    }
    
    public float CurrentThirst
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.currentThirst ?? 0f;
        }
    }
    
    public float CurrentTemp
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.currentTemp ?? 0f;
        }
    }
    
    public float WeatherDuration
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.weatherDuration ?? 0f;
        }
    }
    
    public float WeatherTimer
    {
        get
        {
            WeatherInfo info = stageManager?.GetWeatherInfo(stageID);
            return info?.weatherTimer ?? 0f;
        }
    }
    
    public void Initialize(StageDB db)
    {
        stageManager = FindObjectOfType<StageManager>();
        
        if (db.TryGet(stageID, out stageData))
        {
            Debug.Log($"[Stage] 지역 초기화 성공: ID={stageID}, Name={stageData.name}, Pollution={stageData.stagePollution}");
            
            if (stageManager != null)
            {
                WeatherInfo info = stageManager.GetWeatherInfo(stageID);
                if (info != null && info.weatherDuration == 0f)
                {
                    stageManager.SetWeather(stageID, GetRandomWeatherType());
                }
            }
        }
        else
        {
            Debug.LogError($"[Stage] StageDB에서 ID={stageID}를 찾을 수 없습니다!");
        }
    }
    
    private WeatherType GetRandomWeatherType()
    {
        if (stageData == null) return WeatherType.Sunny;
        
        float totalRate = stageData.sunnyRate + stageData.rainRate + stageData.radioRate + 
                         stageData.snowRate + stageData.thunderRate + stageData.hotRate;
        
        float random = Random.Range(0f, totalRate);
        float accumulated = 0f;
        
        accumulated += stageData.sunnyRate;
        if (random < accumulated) return WeatherType.Sunny;
        
        accumulated += stageData.rainRate;
        if (random < accumulated) return WeatherType.Rain;
        
        accumulated += stageData.radioRate;
        if (random < accumulated) return WeatherType.Radio;
        
        accumulated += stageData.snowRate;
        if (random < accumulated) return WeatherType.Snow;
        
        accumulated += stageData.thunderRate;
        if (random < accumulated) return WeatherType.Thunder;
        
        return WeatherType.Hot;
    }

    public float GetWeatherTimeRemaining()
    {
        return Mathf.Max(0f, WeatherDuration - WeatherTimer);
    }
   
    public float GetWeatherProgress()
    {
        if (WeatherDuration <= 0f) return 1f;
        return Mathf.Clamp01(WeatherTimer / WeatherDuration);
    }
}
