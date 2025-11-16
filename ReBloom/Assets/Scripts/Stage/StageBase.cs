using UnityEngine;
using UnityEngine.InputSystem;

public class StageBase : MonoBehaviour
{
    [Header("Stage Data")]
    public int stageID;
    
    private StageData stageData;
    
    [Header("Weather System")]
    private WeatherType currentWeather;
    private float weatherDuration;
    private float weatherTimer;

    public float CurrentPollution { get; private set; }
    public float CurrentThirst { get; private set; }
    public float CurrentTemp { get; private set; }
    
    public int StageID => stageID;
    public StageData Data => stageData;
    public WeatherType CurrentWeather => currentWeather;

    //디버그용 프로퍼티
    public string StageName => stageData?.name;
    public float WeatherDuration => weatherDuration;
    public float WeatherTimer => weatherTimer;

    public void Initialize(StageDB db)
    {
        if (db.TryGet(stageID, out stageData))
        {
            Debug.Log($"[Stage] 지역 초기화 성공: ID={stageID}, Name={stageData.name}, Pollution={stageData.stagePollution}");

            SetRandomWeather();
        }
        else
        {
            Debug.LogError($"[Stage] StageDB에서 ID={stageID}를 찾을 수 없습니다!");
        }
    }
    
    private void Update()
    {
        if (stageData == null) return;

        weatherTimer += Time.deltaTime;
        
        if (weatherTimer >= weatherDuration)
        {
            SetRandomWeather();
        }
    }
    
    private void SetRandomWeather()
    {
        if (stageData == null) return;
        
        float totalRate = stageData.sunnyRate + stageData.rainRate + stageData.radioRate + 
                         stageData.snowRate + stageData.thunderRate + stageData.hotRate;
        
        float random = Random.Range(0f, totalRate);
        float accumulated = 0f;
        
        accumulated += stageData.sunnyRate;
        if (random < accumulated)
        {
            SetWeather(WeatherType.Sunny);
            return;
        }
        
        accumulated += stageData.rainRate;
        if (random < accumulated)
        {
            SetWeather(WeatherType.Rain);
            return;
        }
        
        accumulated += stageData.radioRate;
        if (random < accumulated)
        {
            SetWeather(WeatherType.Radio);
            return;
        }
        
        accumulated += stageData.snowRate;
        if (random < accumulated)
        {
            SetWeather(WeatherType.Snow);
            return;
        }
        
        accumulated += stageData.thunderRate;
        if (random < accumulated)
        {
            SetWeather(WeatherType.Thunder);
            return;
        }
        
        SetWeather(WeatherType.Hot);
    }

    public void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
        weatherTimer = 0f;
        

        switch (weather)
        {
            case WeatherType.Sunny:
                weatherDuration = stageData.sunny_d + Random.Range(-stageData.sunny_vari, stageData.sunny_vari);
                CurrentPollution = stageData.sunnyPollution;
                CurrentThirst = stageData.sunnyThirst;
                CurrentTemp = stageData.sunnyTemp;
                break;
                
            case WeatherType.Rain:
                weatherDuration = stageData.rain_d + Random.Range(-stageData.rain_vari, stageData.rain_vari);
                CurrentPollution = stageData.rainPollution;
                CurrentThirst = stageData.rainThirst;
                CurrentTemp = stageData.rainTemp;
                break;
                
            case WeatherType.Radio:
                weatherDuration = stageData.radio_d + Random.Range(-stageData.radio_vari, stageData.radio_vari);
                CurrentPollution = stageData.radioPollution;
                CurrentThirst = stageData.radioThirst;
                CurrentTemp = stageData.radioTemp;
                break;
                
            case WeatherType.Snow:
                weatherDuration = stageData.snow_d + Random.Range(-stageData.snow_vari, stageData.snow_vari);
                CurrentPollution = stageData.snowPollution;
                CurrentThirst = stageData.snowThirst;
                CurrentTemp = stageData.snowTemp;
                break;
                
            case WeatherType.Thunder:
                weatherDuration = stageData.thunde_d + Random.Range(-stageData.thunde_vari, stageData.thunde_vari);
                CurrentPollution = stageData.thundePollution;
                CurrentThirst = stageData.thundeThirst;
                CurrentTemp = stageData.thundeTemp;
                break;
                
            case WeatherType.Hot:
                weatherDuration = stageData.hot_d + Random.Range(-stageData.hot_vari, stageData.hot_vari);
                CurrentPollution = stageData.hotPollution;
                CurrentThirst = stageData.hotThirst;
                CurrentTemp = stageData.hotTemp;
                break;
        }
    }

    public float GetWeatherTimeRemaining()
    {
        return Mathf.Max(0f, weatherDuration - weatherTimer);
    }
    

    public float GetWeatherProgress()
    {
        if (weatherDuration <= 0f) return 1f;
        return Mathf.Clamp01(weatherTimer / weatherDuration);
    }
}
