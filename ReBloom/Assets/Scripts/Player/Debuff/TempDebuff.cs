using UnityEngine;

public class TempDebuff : DebuffBase
{

    public TempDebuff(DebuffData data, PlayerStats target)
    : base(data, target) { }

    protected override void OnApply()
    {
    }

    protected override void OnRemove()
    {
    }

    protected override void OnTick(float deltaTime)
    {
        target.Temperature.Modify(data.tempChangePerSec * deltaTime);
    }
}
