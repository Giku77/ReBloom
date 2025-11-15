using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStats : MonoBehaviour
{
    public StatsData data;
    public PlayerEquipManager EquipManager { get; private set; }
    public StatBase Health { get; private set; }
    public StatBase Hunger { get; private set; }
    public StatBase Thirst { get; private set; }
    public StatBase Pollution { get; private set; }
    public StatBase Temperature { get; private set; }

    public event Action<StatBase, float, float> OnStatChanged;

    private void Awake()
    {
        EquipManager = GetComponent<PlayerEquipManager>();

        Health = new HealthStat(this, data.maxHealth);
        
        Hunger = new HungerStat(this, data.hungerMax, data.hungerIncreaseRate);
        Thirst = new ThirstStat(this, data.thurstMax, data.thirstIncreaseRate);
        Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);
    }

    //private void Start()
    //{
    //    Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);
    //}

    private void Update()
    {
        Hunger.Tick();
        Thirst.Tick();              
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


    private void PrintStats()
    {
        Debug.Log("========== 플레이어 상태 ==========");
        Debug.Log($"Health: {Health.Value:F2} / {Health.MaxValue}");
        Debug.Log($"Hunger: {Hunger.Value:F2} / {Hunger.MaxValue}");
        Debug.Log($"Thirst: {Thirst.Value:F2} / {Thirst.MaxValue}");
        Debug.Log($"Pollution: {Pollution.Value:F2} / {Pollution.MaxValue}");
        
        var debuffManager = GetComponent<DebuffManager>();
        if (debuffManager != null)
        {
            var activeDebuffs = debuffManager.GetActiveDebuffs();
            if (activeDebuffs.Count > 0)
            {
                Debug.Log($"\n[활성 디버프] {activeDebuffs.Count}개");
                foreach (var debuff in activeDebuffs)
                {
                    Debug.Log($"  - [{debuff.ID}] {debuff.Name}");
                }
            }
            else
            {
                Debug.Log("\n[활성 디버프] 없음");
            }
        }
        Debug.Log("================================\n");
    }



}
