using UnityEngine;

public class HungerStat : StatBase
{
    private float increaseRate;

    private float actualRate = 0f;

    public float ActualRate => actualRate;

    public HungerStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    { 
        this.increaseRate = increaseRate;
    }

    public override void Tick()
    {
        actualRate = increaseRate;

        Modify(actualRate * Time.deltaTime);
    }
}
