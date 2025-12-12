using UnityEngine;
using UnityEngine.InputSystem;


public class StageDetector : MonoBehaviour
{
    [SerializeField] private RegionDefinition[] regions;
    private StageBase currentStage;
    private StageBase previousStage;

    [Header("Weather FX")]
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject snowEffect;
    [SerializeField] private GameObject radioEffect;
    [SerializeField] private GameObject thunderEffect;

    public StageBase CurrentStage => currentStage;

    private StageManager stageManager;

    private void Start()
    {
        //임시로 시작 구역 거점으로 지정
        //currentStage = startStage;
        stageManager = GetComponent<StageManager>();

        if (currentStage != null)
        {
            ApplyWeather(currentStage.CurrentWeather);
        }
    }

    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintWeathers();
        }
    }

    private void OnEnable()
    {
        StageManager.OnWeatherChange += OnWeatherChanged;
    }

    private void OnDisable()
    {
        StageManager.OnWeatherChange -= OnWeatherChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StageBase>(out StageBase stage))
        {
            previousStage = currentStage != null ? currentStage : stage;
            currentStage = stage;

            ApplyWeather(currentStage.CurrentWeather);

            if (stage.Data != null)
            {
                Debug.Log($"[StageDetector] 지역 진입: {stage.Data.name}");
                if (ToastMessageUI.Instance != null && previousStage != null && previousStage.Data.id == 400 && stage.Data.stagePollution > 0)
                {
                    var stageId = QuestManager.I.Current.goals[0].objectId;
                    var enterName = stageManager.DB.TryGet(stageId, out var questStage) ? questStage.name : "";
                    RegionTitleUI.Instance.ShowRegion(regions[(stage.Data.id % 400) - 1]);
                    if (enterName == stage.Data.name)
                    {
                        QuestManager.I?.PlayQuestCompleteAnimation();
                        QuestManager.I?.ClearPathGuide();
                    }
                    ToastMessageUI.Instance.Show($"오염도 지역에 진입했습니다 : 1초마다 오염도({stage.Data.stagePollution}) 증가");
                }
            }
            else
            {
                Debug.LogWarning($"[StageDetector] Stage ID={stage.StageID}가 초기화되지 않았습니다!");
            }
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Inside"))
        {
            Debug.Log("[StageDetector] 건물 안으로 들어갔습니다.");
            ClearWeatherEffect();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Outside"))
        {
            Debug.Log("[StageDetector] 건물 밖으로 나왔습니다.");
            ApplyWeather(currentStage.CurrentWeather);
        }
    }


    public float GetCurrentPollutionMultiplier()
    {
        if (currentStage != null && currentStage.Data != null)
        {
            return currentStage.Data.stagePollution + currentStage.CurrentPollution; ;
        }

        return 0.0f;
    }

    public float GetCurrentThirst()
    {
        if (currentStage != null && currentStage.Data != null)
        {
            return currentStage.CurrentThirst;
        }
        return 0.0f;
    }

    public float GetCurrentTemperatureMultiplier()
    {
        if (currentStage != null && currentStage.Data != null)
        {
            return currentStage.Data.stageTemp + currentStage.CurrentWeatherTemp + DayNightCycle.Instance.TimeTempDelta;
        }

        //거점 + 맑음 온도 적용
        return 30.0f;
    }

    private void PrintWeathers()
    {
        Debug.Log("========== 현재 날씨 ==========");
        Debug.Log($"Stage: {currentStage.Data.name}");
        Debug.Log($"Weather: {currentStage.CurrentWeather.ToString()}");
        Debug.Log($"Duration: {currentStage.WeatherTimer:F2} /{currentStage.WeatherDuration:F2}");

    }
    private void ApplyWeather(WeatherType weather)
    {
        thunderEffect?.SetActive(false);
        rainEffect?.SetActive(false);
        snowEffect?.SetActive(false);
        radioEffect?.SetActive(false);

        switch (weather)
        {
            case WeatherType.Rain:
                rainEffect?.SetActive(true);
                break;
            case WeatherType.Snow:
                snowEffect?.SetActive(true);
                break;
            case WeatherType.Radio:
                radioEffect?.SetActive(true);
                break;
            case WeatherType.Thunder:
                rainEffect?.SetActive(true);
                thunderEffect?.SetActive(true);
                break;
            case WeatherType.Sunny:
            case WeatherType.Hot:
            default:
                break;
        }
    }

    private void ClearWeatherEffect()
    {
        thunderEffect?.SetActive(false);
        rainEffect?.SetActive(false);
        snowEffect?.SetActive(false);
    }

    private void OnWeatherChanged(int stageID, WeatherType weather)
    {
        if (currentStage != null && currentStage.StageID == stageID)
        {
            ApplyWeather(weather);
        }
    }
}
