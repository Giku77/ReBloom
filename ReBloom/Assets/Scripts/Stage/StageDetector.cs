using UnityEngine;
using UnityEngine.InputSystem;

public class StageDetector : MonoBehaviour
{
    private StageBase currentStage;
    
    public StageBase CurrentStage => currentStage;

    public StageBase startStage = new StageBase(400);

    private void Start()
    {
        //임시로 시작 구역 거점으로 지정
        currentStage = startStage;
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
}
