using UnityEngine;


public class StarvationDebuff : DebuffBase
{
    private float originalSpeed;
    private PlayerController controller;
    
    public StarvationDebuff(DebuffData data, PlayerStats target) 
        : base(data, target) { }
    
    protected override void OnApply()
    {
        controller = target.GetComponent<PlayerController>();
        if (controller != null)
        {
            originalSpeed = controller.moveSpeed;
            controller.moveSpeed *= (1f - data.speedReduce);
            Debug.Log($"[StarvationDebuff] 이동속도 {data.speedReduce * 100}% 감소");
        }
    }
    
    protected override void OnRemove()
    {
        if (controller != null)
        {
            controller.moveSpeed = originalSpeed;
            Debug.Log($"[StarvationDebuff] 이동속도 복구");
        }
    }
    
    protected override void OnTick(float deltaTime)
    {
    }
}