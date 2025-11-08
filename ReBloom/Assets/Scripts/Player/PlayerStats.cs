using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public StatsData data;

    public StatBase Health { get; private set; }
    public StatBase Hunger { get; private set; }
    public StatBase Thirst { get; private set; }
    public StatBase Temperature { get; private set; }

    public event Action<StatBase, float, float> OnStatChanged;
    void Awake()
    {
    }

    void Update()
    {
        //Hunger.Tick();
        //Thirst.Tick();
        //Temperature.Tick();
    }

    public void InvokeStatChanged(StatBase stat, float oldValue, float newValue)
    {
        OnStatChanged?.Invoke(stat, oldValue, newValue);
    }
}
