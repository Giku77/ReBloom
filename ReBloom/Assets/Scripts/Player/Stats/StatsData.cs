using UnityEngine;

[CreateAssetMenu(fileName = "StatsData", menuName = "Scriptable Objects/StatsData")]
public class StatsData : ScriptableObject
{
    [Header("Basics Stats")]
    public float maxHealth = 100f;
    public float hungerMax = 100f;
    public float thurstMax = 100f;
    public float pollutionMax = 100f;

    [Header("Latency Reduction Rate")]
    public float hungerIncreaseRate = 0.02f;
    public float thirstIncreaseRate = 0.25f;

    [Header("Body Temperature Related")]
    public float normalTemperature = 36.5f;

    [Header("Pollution Related")]
    public float pollutionIncreaseRate = 0.5f;

    [Header("Status Effect Damage Rates")]
    public float pollutionDamageRate = 5f;  // 오염도 100일 때 초당 5 데미지
    public float hungerDamageRate = 3f;     // 허기 0일 때 초당 3 데미지
    public float thirstDamageRate = 3f;     // 갈증 100일 때 초당 3 데미지f;
}
