using UnityEngine;
using UnityEngine.InputSystem;

public class ThirstStat : StatBase
{
    private float increaseRate;

    private float actualRate = 0f;
    public float ActualRate => actualRate;

    private StageDetector stageDetector;

    public ThirstStat(PlayerStats owner, float max, float increaseRate) : base(owner, max)
    { 
        this.increaseRate = increaseRate;

        stageDetector = owner.GetComponent<StageDetector>();
    }

    public override void Tick()
    {
        if (stageDetector == null)
            stageDetector = StageDetector.I;

        float weatherRate = stageDetector != null ? stageDetector.GetCurrentThirst(): 0;

        actualRate = increaseRate + weatherRate;

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log($"날씨 갈증 정보: {stageDetector.GetCurrentThirst()}");
            Debug.Log($"[ThirstStat] 기본 증가율: {increaseRate:F2}, 날씨 갈증 계수: {weatherRate}, 최종 증가율: {actualRate:F4}");
        }

        Modify(actualRate * Time.deltaTime);
    }


}
