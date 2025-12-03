using BansheeGz.BGDatabase;
using UnityEngine;
using UnityEngine.InputSystem;

public class TemperatureStat : StatBase
{
    private float normalTemperature = 36.5f;
    private float maxTemperature;
    private float minTemperature;

    private float actualRate = 36.5f;
    public float ActualRate => actualRate;

    private StageDetector stageDetector;

    public TemperatureStat(PlayerStats owner, float normal, float max, float min) : base(owner, max)
    {
        normalTemperature = normal;
        minTemperature = min;

        stageDetector = owner.GetComponent<StageDetector>();

        Set(normal);
    }

    public override void Tick()
    {
        if (stageDetector?.CurrentStage == null) return;

        if (stageDetector.CurrentStage.stageID == 400)
        {
            float target = normalTemperature;
            float changePerSecond = 0.1f;

            float diff = target - value;

            //if (Mathf.Abs(diff) < 0.0001f)
            //    return;

            actualRate = Mathf.Sign(diff) * changePerSecond;

            if (Mathf.Abs(actualRate) > Mathf.Abs(diff))
                actualRate = diff;

            Modify(actualRate * Time.deltaTime);
        }
        //else
        //{
        //    float baseMultiplier = stageDetector.GetCurrentTemperatureMultiplier();

        //    float equipResist = 0f;
        //    if (owner.EquipManager != null)
        //        equipResist = owner.EquipManager.GetTotalInsulationResist();

        //    if (Value > minTemperature)
        //    {
        //      actualRate = (baseMultiplier - Value) * (1 - equipResist) / 120f;
        //      Modify(actualRate * Time.deltaTime);

        //        //임시 확인용
        //        if (Keyboard.current.kKey.wasPressedThisFrame)
        //        {
        //            Debug.Log($"현재온도 정보: {stageDetector.GetCurrentTemperatureMultiplier()}");
        //            Debug.Log($"[TemperatureStat] 현제 체온: {Value:F2}, 장비 단열력: {equipResist:F2} , 최종 증감률: {actualRate:F4}");
        //        }
        //    }
        //}
    }
}
