using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public StatsData data;

    public StatBase Health { get; private set; }
    public StatBase Hunger { get; private set; }
    public StatBase Thirst { get; private set; }
    public StatBase Pollution { get; private set; }
    public StatBase Temperature { get; private set; }

    public event Action<StatBase, float, float> OnStatChanged;

    void Awake()
    {
        Hunger = new HungerStat(this, data.hungerMax, data.hungerDecreaseRate);
        Thirst = new ThirstStat(this, data.thurstMax, data.thirstIncreaseRate);
        Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);

    }

    void Update()
    {
        Hunger.Tick();
        Thirst.Tick();
        Pollution.Tick();
    }

    public void InvokeStatChanged(StatBase stat, float oldValue, float newValue)
    {
        OnStatChanged?.Invoke(stat, oldValue, newValue);
    }
}
