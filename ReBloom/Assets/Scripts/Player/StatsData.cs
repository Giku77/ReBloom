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
    public float hungerDecreaseRate = 0.02f;
    public float thirstIncreaseRate = 0.25f;

    [Header("Body Temperature Related")]
    public float normalTemperature = 36.5f;

    [Header("Pollution Related")]
    public float pollutionIncreaseRate = 0.5f;
}
