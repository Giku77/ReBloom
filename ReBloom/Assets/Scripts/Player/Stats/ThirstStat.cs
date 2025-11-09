using UnityEngine;

public class ThirstStat : StatBase
{
    private float increaseRate;

    public ThirstStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    { 
        this.increaseRate = increaseRate;
    }

    public override void Tick()
    {
        Modify(increaseRate * Time.deltaTime);
    }
}
