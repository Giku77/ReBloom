using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageDetector : MonoBehaviour
{
    private StageBase currentStage;
    
    public StageBase CurrentStage => currentStage;

    private void Start()
    {
        //임시로 시작 구역 거점으로 지정
        //currentStage = startStage;
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
            currentStage = stage;
            
            if (stage.Data != null)
            {
                Debug.Log($"[StageDetector] 지역 진입: {stage.Data.name}");
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

    private void PrintWeathers()
    {
        Debug.Log("========== 현재 날씨 ==========");
        Debug.Log($"Stage: {currentStage.Data.name}");
        Debug.Log($"Weather: {currentStage.CurrentWeather.ToString()}");
        Debug.Log($"Duration: {currentStage.WeatherTimer:F2} /{currentStage.WeatherDuration:F2}");

    }
}
