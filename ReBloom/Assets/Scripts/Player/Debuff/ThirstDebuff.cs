using UnityEngine;

public class ThirstDebuff : DebuffBase
{
    private float originalSpeed;
    private PlayerController controller;
    
    public ThirstDebuff(DebuffData data, PlayerStats target) 
        : base(data, target) { }
    
    protected override void OnApply()
    {
        controller = target.GetComponent<PlayerController>();
        if (controller != null)
        {
            originalSpeed = controller.moveSpeed;
            controller.moveSpeed *= (1f - data.speedReduce);
            Debug.Log($"[ThirstDebuff] 이동속도 {data.speedReduce * 100}% 감소: {originalSpeed} -> {controller.moveSpeed}");
        }
    }
    
    protected override void OnRemove()
    {
        if (controller != null)
        {
            controller.moveSpeed = originalSpeed;
            Debug.Log($"[ThirstDebuff] 이동속도 복구: {controller.moveSpeed}");
        }
    }
    
    protected override void OnTick(float deltaTime)
    {
    }
}