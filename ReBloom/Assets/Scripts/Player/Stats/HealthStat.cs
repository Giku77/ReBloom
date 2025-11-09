using UnityEngine;

public class HealthStat : StatBase
{
    public HealthStat(PlayerStats owner, float max) : base(owner, max)
    { 
    }

    public override void Tick()
    {
        // Health는 자동으로 변하지 않음 (상태이상에 의해서만 변함)
    }
}