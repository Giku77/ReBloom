using UnityEngine;

public class EnvironmentSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private StageDetector stageDetector;
    [SerializeField] private StageManager stageManager;     // WeatherInfo 보관하는 쪽
    [SerializeField] private DayNightCycle dayNight;        // 시간/일차 보관하는 쪽

    public string EntityGuid => "environment";

    private void Reset()
    {
        stageDetector = FindAnyObjectByType<StageDetector>();
        stageManager  = FindAnyObjectByType<StageManager>();
        dayNight      = DayNightCycle.Instance;
    }

    public void Capture(SaveGameDTO save)
    {
        if (save.env == null) save.env = new EnvironmentSaveDTO();

        // 1) 지역
        int stageId = (stageDetector != null && stageDetector.CurrentStage != null)
            ? stageDetector.CurrentStage.StageID
            : 0;

        save.env.currentStageId = stageId;

        // 2) 시간/일차
        if (DayNightCycle.Instance != null)
        {
            save.env.day = DayNightCycle.Instance.CurrentDay;
            save.env.hour = DayNightCycle.Instance.GetCurrentHour();
            save.env.minute = DayNightCycle.Instance.GetCurrentMinute();
        }

        // 3) 날씨(현재 지역 기준)
        if (stageManager != null && stageId != 0)
        {
            var info = stageManager.GetWeatherInfo(stageId);
            if (info != null)
            {
                save.env.weather = info.currentWeather;
                save.env.weatherDuration = info.weatherDuration;
                save.env.weatherTimer = info.weatherTimer;

                save.env.currentPollution = info.currentPollution;
                save.env.currentThirst = info.currentThirst;
                save.env.currentTemp = info.currentTemp;
            }
        }
    }

    public void Restore(SaveGameDTO save)
    {
        if (save?.env == null) return;

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SetTime(save.env.day, save.env.hour, save.env.minute);
        }

        if (stageManager != null && save.env.currentStageId != 0)
        {
            stageManager.SetWeatherRaw(
                save.env.currentStageId,
                save.env.weather,
                save.env.currentPollution,
                save.env.currentThirst,
                save.env.currentTemp,
                save.env.weatherDuration,
                save.env.weatherTimer
            );
        }

        if (stageDetector != null && save.env.currentStageId != 0)
        {
            stageDetector.ForceSetStage(save.env.currentStageId);
        }

    }
}
