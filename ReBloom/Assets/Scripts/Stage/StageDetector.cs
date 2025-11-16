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
            return currentStage.Data.stagePollution; ;
        }

        return 0.0f;
    }

    private void PrintWeathers()
    {
        Debug.Log("========== 현재 지역 및 날씨 ==========");
        Debug.Log($"Stage: {currentStage.StageName}");
        Debug.Log($"Weather: {currentStage.CurrentWeather.ToString()}");
        Debug.Log($"Duration: {currentStage.WeatherDuration:F2}/{currentStage.WeatherTimer:F2}");

    }
}
