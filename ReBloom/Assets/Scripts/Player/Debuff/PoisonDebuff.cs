using UnityEngine;

public class PoisonDebuff : DebuffBase
{
    public PoisonDebuff(DebuffData data, PlayerStats target) 
        : base(data, target) { }
    
    protected override void OnApply()
    {
    }
    
    protected override void OnRemove()
    {
    }
    
    protected override void OnTick(float deltaTime)
    {
    }
}