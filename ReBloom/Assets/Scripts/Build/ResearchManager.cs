using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager I { get; private set; }

    [Header("Debug / 초기값")]
    [SerializeField] private float startProgress = 0f;

    [Header("현재 연구 진척도(읽기 전용)")]
    [SerializeField] private float currentProgress = 0f;
    public float CurrentProgress => currentProgress;

    [Header("Energy")]
    [SerializeField] private float currentEnergy = 0f;
    public float CurrentEnergy => currentEnergy;
    public event Action<float> OnEnergyChanged;

    [Header("Greening")]
    [SerializeField] private float currentGreening = 0f;
    public float CurrentGreening => currentGreening;

    public event Action<float> OnGreeningChanged;


    // 필요하면 UI에서 구독해서 게이지 업데이트
    public event Action<float> OnProgressChanged;

    // 새로 해금된 건축물 알림용 (선택 사항)
    public event Action<ArcData> OnBuildingUnlocked;

    // 이미 해금된 건축물 ID들
    private HashSet<int> unlockedArcIds = new HashSet<int>();

    // 현재 활성화된 건축물 인스턴스들 (필요시 추적용)
    private readonly HashSet<BuildingInstance> activeBuildings = new();

    private CancellationTokenSource researchLoopCts;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        currentProgress = startProgress;
    }

    private void Start()
    {
        RecalculateUnlocks();
        researchLoopCts = new CancellationTokenSource();
        ResearchTickLoop(researchLoopCts.Token).Forget();
    }

    public void RegisterBuilding(BuildingInstance instance)
    {
        if (instance == null) return;
        activeBuildings.Add(instance);
    }

    public void UnregisterBuilding(BuildingInstance instance)
    {
        if (instance == null) return;
        activeBuildings.Remove(instance);
    }

    /// <summary>
    /// 연구 진척도 추가
    /// </summary>
    public void AddProgress(float amount)
    {
        if (amount <= 0f) return;

        currentProgress += amount;
        //Debug.Log($"[Research] Progress += {amount}, now = {currentProgress}");

        OnProgressChanged?.Invoke(currentProgress);
        CheckNewUnlocks();
    }

    public void AddEnergy(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;

        float prev = currentEnergy;

        currentEnergy = Mathf.Max(0f, currentEnergy + amount);

        // 값이 실제로 변했을 때만 이벤트
        if (!Mathf.Approximately(prev, currentEnergy))
            OnEnergyChanged?.Invoke(currentEnergy);
    }


    public void AddGreening(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;

        float prev = currentGreening;
        currentGreening = Mathf.Clamp(currentGreening + amount, 0f, 100f);

        // 값이 실제로 변했을 때만 이벤트
        if (!Mathf.Approximately(prev, currentGreening))
            OnGreeningChanged?.Invoke(currentGreening);
    }


    /// <summary>
    /// 디버그용: 모든 건축 해금 가능한 수준까지 한번에 채우기
    /// </summary>
    public void DebugFillToMax()
    {
        var allArcs = BuildManager.I.ArcDB.GetAll();
        float maxUnlock = allArcs.Values.Max(a => a.unlockValue);

        currentProgress = maxUnlock;
        Debug.Log($"[Research] DebugFillToMax -> {currentProgress}");

        RecalculateUnlocks();
        OnProgressChanged?.Invoke(currentProgress);
    }

    /// <summary>
    /// 이 건축물이 현재 해금 상태인지?
    /// UnlockValue == 0 이면 항상 해금으로 취급
    /// </summary>
    public bool IsUnlocked(ArcData arc)
    {
        if (arc.unlockValue <= 0) return true;
        return unlockedArcIds.Contains(arc.arcId);
    }

    private async UniTaskVoid ResearchTickLoop(CancellationToken token)
    {
        var arcDB = BuildManager.I?.ArcDB;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
            }
            catch (OperationCanceledException) { break; }

            if (BuildManager.I == null || arcDB == null || !arcDB.IsLoaded) continue;

            float totalResearch = 0f;
            float totalEnergy = 0f;
            float totalGreen = 0f;

            foreach (var inst in BuildManager.I.EnumerateAllInstances())
            {
                if (inst == null) continue;
                if (!arcDB.TryGet(inst.arcId, out var arc)) continue;

                totalResearch += Mathf.Max(0f, arc.researchInc);
                totalEnergy   += (arc.energyInc - arc.energyDec);
                totalGreen    += arc.greeningInc;
            }

            if (totalResearch > 0f) AddProgress(totalResearch);
            if (!Mathf.Approximately(totalEnergy, 0f)) AddEnergy(totalEnergy);
            if (!Mathf.Approximately(totalGreen, 0f)) AddGreening(totalGreen); // 내부에서 0~100 Clamp
        }
    }


    // ----------------- 내부 로직 -----------------

    private void RecalculateUnlocks()
    {
        unlockedArcIds.Clear();

        var allArcs = BuildManager.I.ArcDB.GetAll();
        foreach (var arc in allArcs)
        {
            if (arc.Value.unlockValue <= 0) continue;

            if (currentProgress >= arc.Value.unlockValue)
            {
                unlockedArcIds.Add(arc.Key);
            }
        }
    }

    private void CheckNewUnlocks()
    {
        var allArcs = BuildManager.I.ArcDB.GetAll();

        foreach (var arc in allArcs)
        {
            if (arc.Value.unlockValue <= 0) continue;

            // 아직 안 열린 애 + 조건 만족하면 새로 해금
            if (currentProgress >= arc.Value.unlockValue &&
                unlockedArcIds.Add(arc.Key))
            {
                Debug.Log($"[Research] New building unlocked: {arc.Value.name}");
                OnBuildingUnlocked?.Invoke(arc.Value);
            }
        }
    }

    public ResearchSaveDTO Capture()
    {
        return new ResearchSaveDTO
        {
            energy = CurrentEnergy,
            progress = CurrentProgress,
            greening = CurrentGreening,
        };
    }

    public void Apply(ResearchSaveDTO dto)
    {
        if (dto == null) return;

        SetEnergy(dto.energy);
        SetProgress(dto.progress);
        SetGreening(dto.greening);
    }

    public void SetProgress(float value, bool notify = true)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentProgress, value)) return;

        currentProgress = value;

        RecalculateUnlocks();

        if (notify)
            OnProgressChanged?.Invoke(currentProgress);

        //AutoSaveService.I?.RequestSave("ResearchProgressSet");
    }

    public void SetEnergy(float value, bool notify = true)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentEnergy, value)) return;

        currentEnergy = value;

        if (notify)
            OnEnergyChanged?.Invoke(currentEnergy);

        //AutoSaveService.I?.RequestSave("ResearchEnergySet");
    }

    public void SetGreening(float value, bool notify = true)
    {
        value = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(currentGreening, value)) return;

        currentGreening = value;

        if (notify)
            OnGreeningChanged?.Invoke(currentGreening);

        //AutoSaveService.I?.RequestSave("ResearchGreeningSet");
    }

}
