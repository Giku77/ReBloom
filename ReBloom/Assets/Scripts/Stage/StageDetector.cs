using UnityEngine;
using UnityEngine.InputSystem;

public class StageDetector : MonoBehaviour
{
    private StageBase currentStage;
    private StageBase previousStage;
    
    public StageBase CurrentStage => currentStage;

    private StageManager stageManager;

    private void Start()
    {
        //임시로 시작 구역 거점으로 지정
        //currentStage = startStage;
        stageManager = GetComponent<StageManager>();
    }

    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintWeathers();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StageBase>(out StageBase stage))
        {
            previousStage = currentStage != null ? currentStage : stage;
            currentStage = stage;
            
            if (stage.Data != null)
            {
                Debug.Log($"[StageDetector] 지역 진입: {stage.Data.name}");
                if (ToastMessageUI.Instance != null && previousStage != null && previousStage.Data.id == 400 && stage.Data.stagePollution > 0)
                {
                    var stageId = QuestManager.I.Current.goals[0].objectId;
                    var enterName = stageManager.DB.TryGet(stageId, out var questStage) ? questStage.name : "";
                    if (enterName == stage.Data.name)
                    {
                        QuestManager.I?.TryCompleteCurrent();
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
            return currentStage.Data.stageTemp + currentStage.CurrentTemp;
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
}
