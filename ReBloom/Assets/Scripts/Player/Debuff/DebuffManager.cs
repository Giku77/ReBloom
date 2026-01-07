using System;
using System.Collections.Generic;
using UnityEngine;


public class DebuffManager : MonoBehaviour
{
    private PlayerStats playerStats;
    private DebuffDB debuffDB;
    
    private List<IDebuff> activeDebuffs = new List<IDebuff>();

    public event Action<IDebuff> OnDebuffApplied;
    public event Action<IDebuff> OnDebuffRemoved;
    private Dictionary<int, Func<DebuffData, IDebuff>> debuffFactory;

    private StageDetector stageDetector;


    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        debuffDB = new DebuffDB();
        stageDetector = GetComponent<StageDetector>();
        if (stageDetector == null)
            stageDetector = StageDetector.I;


        InitializeFactory();
        LoadDebuffData();
    }

    private void InitializeFactory()
    {
        debuffFactory = new Dictionary<int, Func<DebuffData, IDebuff>>()
        {
            // 중독 (오염도)
            { 210, (data) => new PoisonDebuff(data, playerStats) },
            
            // 갈증
            { 220, (data) => new ThirstDebuff(data, playerStats) },
            { 221, (data) => new ThirstDebuff(data, playerStats) },
            { 222, (data) => new ThirstDebuff(data, playerStats) },
            
            // 허기
            { 230, (data) => new StarvationDebuff(data, playerStats) },
            { 231, (data) => new StarvationDebuff(data, playerStats) },
            { 232, (data) => new StarvationDebuff(data, playerStats) },

            //체온
            {240, (data) => new hypothermiaDebuff(data, playerStats) },
            {250, (data) => new hypothermiaDebuff(data, playerStats) },
            {260, (data) => new hypothermiaDebuff(data, playerStats) },
            {270, (data) => new hypothermiaDebuff(data, playerStats) },

            //온도
            {280, (data) => new  TempDebuff(data, playerStats)},
            {281, (data) => new  TempDebuff(data, playerStats)},
            {282, (data) => new  TempDebuff(data, playerStats)},
            {283, (data) => new  TempDebuff(data, playerStats)},
            {284, (data) => new  TempDebuff(data, playerStats)},
            {285, (data) => new  TempDebuff(data, playerStats)},
            {286, (data) => new  TempDebuff(data, playerStats)},
            {287, (data) => new  TempDebuff(data, playerStats)},



        };
    }

    private void LoadDebuffData()
    {
        debuffDB.LoadFromBG();
    }

    private void Update()
    {
        if (playerStats.StatDebugMode || playerStats.DebugMode)
            return;

        for (int i = activeDebuffs.Count - 1; i >= 0; i--)
        {
            var debuff = activeDebuffs[i];
            debuff.Tick(Time.deltaTime);
            
            if (debuff.ShouldRemove())
            {
                RemoveDebuff(debuff);
            }
        }
        
        CheckStatThresholds();
    }

    private void CheckStatThresholds()
    {
        CheckPollutionThreshold();
        CheckThirstThreshold();
        CheckHungerThreshold();
        CheckTemperatureThreshold();
        CheckFieldTempThreshold();
    }

    private void CheckPollutionThreshold()
    {
        float pollution = playerStats.Pollution.Value;
        
        if (pollution >= 100 && !HasDebuff(210))
        {
            ApplyDebuff(210);
        }
        else if (pollution < 100 && HasDebuff(210))
        {
            RemoveDebuffByID(210);
        }
    }

    private void CheckThirstThreshold()
    {
        float thirst = playerStats.Thirst.Value;
        
        if (thirst >= 100 && !HasDebuff(222))
        {
            RemoveDebuffByID(220);
            RemoveDebuffByID(221);
            ApplyDebuff(222);
        }
        else if (thirst >= 50 && thirst < 100 && !HasDebuff(221))
        {
            RemoveDebuffByID(220);
            RemoveDebuffByID(222);
            ApplyDebuff(221);
        }
        else if (thirst >= 30 && thirst < 50 && !HasDebuff(220))
        {
            RemoveDebuffByID(221);
            RemoveDebuffByID(222);
            ApplyDebuff(220);
        }
        else if (thirst < 30)
        {
            RemoveDebuffByID(220);
            RemoveDebuffByID(221);
            RemoveDebuffByID(222);
        }
    }
    
    private void CheckHungerThreshold()
    {
        float hunger = playerStats.Hunger.Value;
        
        if (hunger >= 100 && !HasDebuff(232))
        {
            RemoveDebuffByID(230);
            RemoveDebuffByID(231);
            ApplyDebuff(232);
        }
        else if (hunger >= 50 && hunger < 100 && !HasDebuff(231))
        {
            RemoveDebuffByID(230);
            RemoveDebuffByID(232);
            ApplyDebuff(231);
        }
        else if (hunger >= 30 && hunger < 50 && !HasDebuff(230))
        {
            RemoveDebuffByID(231);
            RemoveDebuffByID(232);
            ApplyDebuff(230);
        }
        else if (hunger < 30)
        {
            RemoveDebuffByID(230);
            RemoveDebuffByID(231);
            RemoveDebuffByID(232);
        }
    }

    private void CheckTemperatureThreshold()
    {
        float temperature = playerStats.Temperature.Value;

        if (temperature >= 41 && !HasDebuff(270))
        {
            RemoveDebuffByID(260);
            RemoveDebuffByID(250);
            RemoveDebuffByID(240);
            ApplyDebuff(270);
        }
        else if (temperature >= 38 && temperature < 41 && !HasDebuff(260))
        {
            RemoveDebuffByID(270);
            RemoveDebuffByID(250);
            RemoveDebuffByID(240);
            ApplyDebuff(260);
        }
        else if (temperature <= 31 && !HasDebuff(250))
        {
            RemoveDebuffByID(270);
            RemoveDebuffByID(260);
            RemoveDebuffByID(240);
            ApplyDebuff(250);
        }
        else if (temperature > 31 && temperature < 34 && !HasDebuff(240))
        {
            RemoveDebuffByID(270);
            RemoveDebuffByID(260);
            RemoveDebuffByID(250);
            ApplyDebuff(240);
        }
        else if (temperature >= 34 && temperature < 38)
        {
            RemoveDebuffByID(270);
            RemoveDebuffByID(260);
            RemoveDebuffByID(250);
            RemoveDebuffByID(240);
        }
    }

    private void CheckFieldTempThreshold()
    {
        float fieldTemp = stageDetector.GetCurrentTemperatureMultiplier();

        // 추위 4단계: -1°C 미만
        if (fieldTemp < -1 && !HasDebuff(284))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(284);
        }
        // 추위 3단계: -1~5°C
        else if (fieldTemp >= -1 && fieldTemp < 5 && !HasDebuff(283))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(283);
        }
        // 추위 2단계: 5~12°C
        else if (fieldTemp >= 5 && fieldTemp < 12 && !HasDebuff(282))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(282);
        }
        // 추위 1단계: 12~20°C
        else if (fieldTemp >= 12 && fieldTemp < 20 && !HasDebuff(281))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(281);
        }
        // 온화: 20~32.1°C
        else if (fieldTemp >= 20 && fieldTemp <= 32.1f && !HasDebuff(280))
        {
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(280);
        }
        // 더위 1단계: 32.1~37°C
        else if (fieldTemp > 32.1f && fieldTemp < 37 && !HasDebuff(285))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(286);
            RemoveDebuffByID(287);
            ApplyDebuff(285);
        }
        // 더위 2단계: 37~45°C
        else if (fieldTemp >= 37 && fieldTemp < 45 && !HasDebuff(286))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(287);
            ApplyDebuff(286);
        }
        // 더위 3단계: 45°C 이상
        else if (fieldTemp >= 45 && !HasDebuff(287))
        {
            RemoveDebuffByID(280);
            RemoveDebuffByID(281);
            RemoveDebuffByID(282);
            RemoveDebuffByID(283);
            RemoveDebuffByID(284);
            RemoveDebuffByID(285);
            RemoveDebuffByID(286);
            ApplyDebuff(287);
        }
    }


    public void ApplyDebuff(int debuffID)
    {
        if (!debuffDB.TryGet(debuffID, out DebuffData data))
        {
            Debug.LogError($"[DebuffManager] Debuff ID {debuffID}를 데이터에서 찾을 수 없습니다!");
            return;
        }
        
        if (!data.isMultiAble && HasDebuff(debuffID))
        {
            return;
        }
        
        if (debuffFactory.ContainsKey(debuffID))
        {
            var debuff = debuffFactory[debuffID](data);
            debuff.Apply(playerStats);
            activeDebuffs.Add(debuff);
            
            OnDebuffApplied?.Invoke(debuff);
        }
        else
        {
            Debug.LogWarning($"[DebuffManager] Debuff ID {debuffID}에 대한 팩토리가 없습니다.");
        }
    }

    public void RemoveDebuff(IDebuff debuff)
    {
        debuff.Remove(playerStats);
        activeDebuffs.Remove(debuff);
        
        OnDebuffRemoved?.Invoke(debuff);
    }
    
    public void RemoveDebuffByID(int debuffID)
    {
        var debuff = activeDebuffs.Find(e => e.ID == debuffID);
        if (debuff != null)
        {
            RemoveDebuff(debuff);
        }
    }
    
    public bool HasDebuff(int debuffID)
    {
        return activeDebuffs.Exists(e => e.ID == debuffID);
    }

    public List<IDebuff> GetActiveDebuffs()
    {
        return activeDebuffs;
    }
}