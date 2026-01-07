using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageDetector : MonoBehaviour
{
    [SerializeField] private RegionDefinition[] regions;

    public static StageDetector I { get; private set; }

    public static event Action<bool> OnEnterDoor;
    public static event Action<int> OnStageChanged;

    private StageManager stageManager;
    private StageService stageService;

    public StageBase CurrentStage => stageService != null ? stageService.CurrentStage : null;
    public bool CanBuild => stageService != null && stageService.CanBuild;

    private void Awake()
    {
        I = this;
        stageService = StageService.I != null ? StageService.I : FindFirstObjectByType<StageService>();
    }

    private void Start()
    {
        stageManager = GetComponent<StageManager>();
    }

    private void OnEnable()
    {
        if (stageService != null)
        {
            stageService.OnStageChanged += HandleStageChanged;
            stageService.OnInsideChanged += HandleInsideChanged;
        }

        StageManager.OnWeatherChange += OnWeatherChanged;
        PlayerController.OnResurrection += PlaceOutDoor;
    }

    public void ForceSetStage(int stageId)
    {
        if (stageService == null)
            stageService = StageService.I != null ? StageService.I : FindFirstObjectByType<StageService>();

        stageService?.ForceSetStage(stageId);
    }


    private void OnDisable()
    {
        if (stageService != null)
        {
            stageService.OnStageChanged -= HandleStageChanged;
            stageService.OnInsideChanged -= HandleInsideChanged;
        }

        StageManager.OnWeatherChange -= OnWeatherChanged;
        PlayerController.OnResurrection -= PlaceOutDoor;
    }

    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
            PrintWeathers();
    }

    private void HandleInsideChanged(bool inside)
    {
        OnEnterDoor?.Invoke(inside);
    }

    private void HandleStageChanged(StageBase prev, StageBase cur)
    {
        if (cur == null) return;

        if (cur.Data != null)
        {
            Debug.Log($"[StageDetector] 지역 진입: {cur.Data.name}");

            // 여기 조건은 너 기존 코드 그대로 유지
            if (ToastMessageUI.Instance != null &&
                prev != null && prev.Data != null &&
                prev.Data.id == 400 &&
                cur.Data.stagePollution > 0)
            {
                var stageId = QuestManager.I.Current.goals[0].objectId;
                var enterName = stageManager.DB.TryGet(stageId, out var questStage) ? questStage.name : "";

                RegionTitleUI.Instance.ShowRegion(regions[(cur.Data.id % 400) - 1]);

                if (enterName == cur.Data.name)
                {
                    QuestManager.I?.PlayQuestCompleteAnimation();
                    QuestManager.I?.ClearPathGuide();
                }

                ToastMessageUI.Instance.Show($"오염도 지역에 진입했습니다 : 1초마다 오염도({cur.Data.stagePollution}) 증가");

                OnStageChanged?.Invoke(cur.StageID);
                AutoSaveService.I?.RequestSave($"StageChanged:{cur.StageID}");
                SoundManager.I?.PlayAreaTransition();
            }
        }
        else
        {
            Debug.LogWarning($"[StageDetector] Stage ID={cur.StageID}가 초기화되지 않았습니다!");
        }
    }

    public float GetCurrentPollutionMultiplier()
        => stageService != null ? stageService.GetCurrentPollutionMultiplier() : 0f;

    public float GetCurrentThirst()
        => stageService != null ? stageService.GetCurrentThirst() : 0f;

    public float GetCurrentTemperatureMultiplier()
    {
        var cur = CurrentStage;
        if (cur != null && cur.Data != null)
            return cur.Data.stageTemp + cur.CurrentWeatherTemp + DayNightCycle.Instance.TimeTempDelta;

        return 30.0f;
    }

    private void PrintWeathers()
    {
        var cur = CurrentStage;
        if (cur?.Data == null) return;

        Debug.Log("========== 현재 날씨 ==========");
        Debug.Log($"Stage: {cur.Data.name}");
        Debug.Log($"Weather: {cur.CurrentWeather}");
        Debug.Log($"Duration: {cur.WeatherTimer:F2} /{cur.WeatherDuration:F2}");
    }

    private void OnWeatherChanged(int stageID, WeatherType weather)
    {
        var cur = CurrentStage;
        if (cur != null && cur.StageID == stageID)
        {
            AutoSaveService.I?.RequestSave($"WeatherChanged:{stageID}:{weather}");
        }
    }

    private void PlaceOutDoor()
    {
        // 부활 시 실내 상태 강제로 밖으로
        stageService?.SetCanBuild(true);
        stageService?.SetInside(false);
    }
}
