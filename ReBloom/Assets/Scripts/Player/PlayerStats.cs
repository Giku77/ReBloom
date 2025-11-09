using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
        Health = new HealthStat(this, data.maxHealth);
        
        Hunger = new HungerStat(this, data.hungerMax, data.hungerIncreaseRate);
        Thirst = new ThirstStat(this, data.thurstMax, data.thirstIncreaseRate);
        Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);

    }

    void Update()
    {
        Hunger.Tick();
        Thirst.Tick();
        
        CheckStatusEffects();
        Pollution.Tick();

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintStats();
        }
    }

    public void InvokeStatChanged(StatBase stat, float oldValue, float newValue)
    {
        OnStatChanged?.Invoke(stat, oldValue, newValue);
    }


void PrintStats()
    {
        Debug.Log($"Health: {Health.Value:F2} / {Health.MaxValue}"); 
        Debug.Log($"Hunger: {Hunger.Value:F2} / {Hunger.MaxValue}");
        Debug.Log($"Thirst: {Thirst.Value:F2} / {Thirst.MaxValue}");
        Debug.Log($"Pollution: {Pollution.Value:F2} / {Pollution.MaxValue}");
    }


void CheckStatusEffects()
    {
        // 오염도가 최대치일 때 체력 감소
        if (Pollution.Value >= Pollution.MaxValue)
        {
            Health.Modify(-data.pollutionDamageRate * Time.deltaTime);
        }
        
        // 허기가 0일 때 체력 감소
        if (Hunger.Value <= 0)
        {
            Health.Modify(-data.hungerDamageRate * Time.deltaTime);
        }
        
        // 갈증이 최대치일 때 체력 감소
        if (Thirst.Value >= Thirst.MaxValue)
        {
            Health.Modify(-data.thirstDamageRate * Time.deltaTime);
        }
    }
}
