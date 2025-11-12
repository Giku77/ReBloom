using UnityEngine;

public class StageDetector : MonoBehaviour
{
    private StageBase currentStage;
    
    public StageBase CurrentStage => currentStage;
    
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
    
    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.TryGetComponent<StageBase>(out StageBase stage) && currentStage == stage)
    //    {
    //        if (stage.Data != null)
    //        {
    //            Debug.Log($"[StageDetector] 지역 퇴장: {stage.Data.name}");
    //        }
    //        currentStage = null;
    //    }
    //}
    
    public float GetCurrentPollutionMultiplier()
    {
        if (currentStage != null && currentStage.Data != null)
        {
            //return currentStage.Data.stagePollution;
            return 10f;
        }
        return 0.0f;
    }
}
