using UnityEngine;

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
        float multiplier = stageDetector != null ? stageDetector.GetCurrentPollutionMultiplier() : 0f;
        //float actualRate = baseIncreaseRate + multiplier;

        float actualRate = multiplier;

        if(stageDetector.CurrentStage.Data.id == 400)
            actualRate = -5f;

        //Debug.Log(actualRate);

        Modify(actualRate * Time.deltaTime);
    }
}