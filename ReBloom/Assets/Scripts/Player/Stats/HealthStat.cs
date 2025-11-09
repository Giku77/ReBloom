using UnityEngine;

public class HealthStat : StatBase
{
    public HealthStat(PlayerStats owner, float max) : base(owner, max)
    {
        this.value = max;
    }

    public override void Tick()
    {
    }
}