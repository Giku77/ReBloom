using UnityEngine;

public class PollutionStat : StatBase
{
    private float increaseRate;

    public PollutionStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    {
        this.increaseRate = increaseRate;
        this.value = 0;
    }

    public override void Tick()
    {
        Modify(increaseRate * Time.deltaTime);
    }
}
