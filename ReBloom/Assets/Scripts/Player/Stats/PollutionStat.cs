using UnityEngine;
using UnityEngine.InputSystem;

public class PollutionStat : StatBase
{
    //private float baseIncreaseRate;
    private StageDetector stageDetector;

    public PollutionStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    {
        //this.baseIncreaseRate = increaseRate;
        
        stageDetector = owner.GetComponent<StageDetector>();
    }

public override void Tick()
    {
        float baseMultiplier = stageDetector != null ? stageDetector.GetCurrentPollutionMultiplier() : 0f;

        float equipResist = 0f;
        if (owner.EquipManager != null)
            equipResist = owner.EquipManager.GetTotalPollutionResist();

        float actualRate = (1f - equipResist) * baseMultiplier;

        if (stageDetector.CurrentStage != null && stageDetector.CurrentStage.Data.id == 400)
            actualRate = -5f;

        //임시 확인용
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log($"지역오염도 정보: {stageDetector.GetCurrentPollutionMultiplier()}");
            Debug.Log($"[PollutionStat] 기본 증가율: {baseMultiplier:F2}, 장비 저항: {equipResist:F4} ({equipResist * 100:F2}%), 최종 증가율: {actualRate:F4}");
        }

        Modify(actualRate * Time.deltaTime);
    }
}