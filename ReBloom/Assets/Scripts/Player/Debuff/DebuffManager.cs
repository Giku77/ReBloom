using System;
using System.Collections.Generic;
using UnityEngine;


public class DebuffManager : MonoBehaviour
{
    private PlayerStats playerStats;
    private DebuffDB debuffDB;
    
    private List<IDebuff> activeDebuffs = new List<IDebuff>();
    
    private Dictionary<int, Func<DebuffData, IDebuff>> debuffFactory;
    
    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        debuffDB = new DebuffDB();
        
        InitializeFactory();
        LoadDebuffData();
    }
    
    void InitializeFactory()
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
        };
    }
    
    void LoadDebuffData()
    {
        debuffDB.LoadFromBG();
    }
    
    void Update()
    {
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
    
    void CheckStatThresholds()
    {
        CheckPollutionThreshold();
        CheckThirstThreshold();
        CheckHungerThreshold();
    }
    
    void CheckPollutionThreshold()
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
    
    void CheckThirstThreshold()
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
    
    void CheckHungerThreshold()
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