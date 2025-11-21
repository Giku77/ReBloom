using UnityEngine;

public class hypothermiaDebuff : DebuffBase
{
    private PlayerController controller;
    private float multiplier;

    public hypothermiaDebuff(DebuffData data, PlayerStats target)
       : base(data, target) { }

    protected override void OnApply()
    {
        controller = target.GetComponent<PlayerController>();
        if (controller != null)
        {
            multiplier = 1f - data.speedReduce;
            controller.AddSpeedMultiplier(this, multiplier);
            Debug.Log($"[StarvationDebuff] 이동속도 {data.speedReduce * 100}% 감소");
        }
    }

    protected override void OnRemove()
    {
        if (controller != null)
        {
            controller.RemoveSpeedMultiplier(this);
            Debug.Log($"[StarvationDebuff] 이동속도 복구: {controller.moveSpeed}");
        }
    }

    protected override void OnTick(float deltaTime)
    {
        
    }
}
