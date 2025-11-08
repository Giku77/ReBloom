using UnityEngine;

public class HungerStat : StatBase
{
    private float decreaseRate;

    public HungerStat(PlayerStats owner, float max, float decreaseRate) : base(owner, max)
    { 
        this.decreaseRate = decreaseRate;
    }

    public override void Tick()
    {
        Modify(-decreaseRate * Time.deltaTime);
    }
}
