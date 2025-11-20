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
        Debug.Log($"[Research] Progress += {amount}, now = {currentProgress}");

        OnProgressChanged?.Invoke(currentProgress);
        CheckNewUnlocks();
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
        var arcDB = BuildManager.I.ArcDB;

        while (!token.IsCancellationRequested)
        {
            try
            {
                // 1초마다 한 번씩
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (arcDB == null) continue;

            float totalInc = 0f;

            // 활성 건물들 돌면서 연구량 합산
            foreach (var inst in activeBuildings)
            {
                if (inst == null) continue;

                if (!arcDB.TryGet(inst.arcId, out var arc)) continue;

                if (arc.researchInc <= 0f) continue;

                totalInc += arc.researchInc;
            }

            if (totalInc > 0f)
            {
                AddProgress(totalInc);
            }
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
}
