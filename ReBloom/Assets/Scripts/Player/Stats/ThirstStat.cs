using UnityEngine;

public class ThirstStat : StatBase
{
    private float increaseRate;

    public ThirstStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    { 
        this.increaseRate = increaseRate;
        this.value = 0;
    }

    public override void Tick()
    {
        Modify(increaseRate * Time.deltaTime);
    }
}
