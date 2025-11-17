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

    public event Action OnDeath;

    private bool isDead = false;

    [SerializeField] private bool isDebug = true;

    public bool DebugMode { get; set; } = false;

    private void Awake()
    {
        EquipManager = GetComponent<PlayerEquipManager>();

        Health = new HealthStat(this, data.maxHealth);
        
        Hunger = new HungerStat(this, data.hungerMax, data.hungerIncreaseRate);
        Thirst = new ThirstStat(this, data.thurstMax, data.thirstIncreaseRate);
        Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);
        Temperature = new TemperatureStat(this, data.normalTemperature, data.maxTemperature, data.minTemperature);

        //LSY: DeathBoxEvent
        RegisterToDeathBoxHandler();
    }

    //private void Start()
    //{
    //    Pollution = new PollutionStat(this, data.pollutionMax, data.pollutionIncreaseRate);
    //}

    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintStats();
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            Health.Modify(-10f);
        }

        if (DebugMode)
        return;

        Hunger.Tick();
        Thirst.Tick();              
        Pollution.Tick();
        Temperature.Tick();

        if (Health.Value <= 0 && !isDead && !isDebug)
        {
            isDead = true;

            GetResurrection();
            OnDeath?.Invoke();
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
        Debug.Log($"Temperature: {Temperature.Value:F2} / {Temperature.MaxValue}");
        
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

    private void GetResurrection()
    {
        Health.Set(50f);
        Hunger.Set(50f);
        Pollution.Set(0f);
        Thirst.Set(50f);
        Temperature.Set(36.5f);
    }

    /// <summary>
    /// LSY: DeathBoxHandler를 찾아서 자동 이벤트 등록
    /// </summary>
    private void RegisterToDeathBoxHandler()
    {
        DeathBoxHandler handler = FindFirstObjectByType<DeathBoxHandler>();

        if (handler != null)
        {
            // 기존 구독 해제 (중복 방지)
            OnDeath -= handler.OnCreateDeathBox;
            // 새로 구독
            OnDeath += handler.OnCreateDeathBox;

            Debug.Log("[PlayerStats] DeathBoxHandler 이벤트 등록 완료");
        }
        else
        {
            Debug.LogWarning("[PlayerStats] DeathBoxHandler를 찾을 수 없습니다!");
        }
    }
}
