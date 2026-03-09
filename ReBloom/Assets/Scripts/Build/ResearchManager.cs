using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager I { get; private set; }

    [Header("Debug / Initial Value")]
    [SerializeField] private float startProgress = 0f;

    [Header("Current Research Progress")]
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

    public event Action<float> OnProgressChanged;
    public event Action<ArcData> OnBuildingUnlocked;

    private readonly HashSet<int> unlockedArcIds = new();
    private readonly HashSet<BuildingInstance> activeBuildings = new();

    private CancellationTokenSource researchLoopCts;
    private PlayerRegistry boundRegistry;
    private bool networkBindingsActive;
    private bool lastNetworkedSession;
    private bool lastServerAuthority;

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    private bool HasServerAuthority => !IsNetworkedSession || NetworkManager.Singleton.IsServer;

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
        StartResearchLoopIfNeeded();
        BindNetworkStateAsync().Forget();
        WaitForBuildManagerReadyAsync().Forget();
        lastNetworkedSession = IsNetworkedSession;
        lastServerAuthority = HasServerAuthority;
    }

    private void Update()
    {
        bool networked = IsNetworkedSession;
        bool serverAuthority = HasServerAuthority;

        if (networked != lastNetworkedSession || serverAuthority != lastServerAuthority)
        {
            HandleSessionModeChanged(networked, serverAuthority);
            lastNetworkedSession = networked;
            lastServerAuthority = serverAuthority;
        }

        if (networked && !serverAuthority && (!networkBindingsActive || boundRegistry == null || !boundRegistry || !boundRegistry.IsSpawned))
        {
            BindNetworkStateAsync().Forget();
        }
    }

    private void OnDestroy()
    {
        if (I == this)
            I = null;

        StopResearchLoop();
        TeardownNetworkBindings();
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

    public void AddProgress(float amount)
    {
        if (!HasServerAuthority || amount <= 0f)
            return;

        float next = currentProgress + amount;
        if (SetProgressInternal(next, true))
            CheckNewUnlocks();
    }

    public void AddEnergy(float amount)
    {
        if (!HasServerAuthority || Mathf.Approximately(amount, 0f))
            return;

        SetEnergyInternal(currentEnergy + amount, true);
    }

    public void AddGreening(float amount)
    {
        if (!HasServerAuthority || Mathf.Approximately(amount, 0f))
            return;

        SetGreeningInternal(currentGreening + amount, true);
    }

    public void DebugFillToMax()
    {
        if (!HasServerAuthority)
            return;

        var allArcs = BuildManager.I?.ArcDB?.GetAll();
        if (allArcs == null || allArcs.Count == 0)
            return;

        float maxUnlock = allArcs.Values.Max(a => a.unlockValue);
        if (SetProgressInternal(maxUnlock, true))
            CheckNewUnlocks();

        Debug.Log($"[Research] DebugFillToMax -> {currentProgress}");
    }

    public bool IsUnlocked(ArcData arc)
    {
        if (arc.unlockValue <= 0) return true;
        return unlockedArcIds.Contains(arc.arcId);
    }

    private async UniTaskVoid ResearchTickLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!HasServerAuthority)
                continue;

            var arcDB = BuildManager.I?.ArcDB;
            if (BuildManager.I == null || arcDB == null || !arcDB.IsLoaded)
                continue;

            float totalResearch = 0f;
            float totalEnergy = 0f;
            float totalGreen = 0f;

            foreach (var inst in BuildManager.I.EnumerateAllInstances())
            {
                if (inst == null) continue;
                if (!arcDB.TryGet(inst.arcId, out var arc)) continue;

                totalResearch += Mathf.Max(0f, arc.researchInc);
                totalEnergy += arc.energyInc - arc.energyDec;
                totalGreen += arc.greeningInc;
            }

            if (totalResearch > 0f) AddProgress(totalResearch);
            if (!Mathf.Approximately(totalEnergy, 0f)) AddEnergy(totalEnergy);
            if (!Mathf.Approximately(totalGreen, 0f)) AddGreening(totalGreen);
        }
    }

    private void RecalculateUnlocks()
    {
        unlockedArcIds.Clear();

        var arcDB = BuildManager.I?.ArcDB;
        if (arcDB == null || !arcDB.IsLoaded)
            return;

        foreach (var arc in arcDB.GetAll())
        {
            if (arc.Value.unlockValue <= 0) continue;
            if (currentProgress >= arc.Value.unlockValue)
                unlockedArcIds.Add(arc.Key);
        }
    }

    private void CheckNewUnlocks()
    {
        var arcDB = BuildManager.I?.ArcDB;
        if (arcDB == null || !arcDB.IsLoaded)
            return;

        foreach (var arc in arcDB.GetAll())
        {
            if (arc.Value.unlockValue <= 0) continue;

            if (currentProgress >= arc.Value.unlockValue && unlockedArcIds.Add(arc.Key))
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
        if (dto == null || !HasServerAuthority)
            return;

        SetEnergyInternal(dto.energy, true);
        SetProgressInternal(dto.progress, true);
        SetGreeningInternal(dto.greening, true);
    }

    public void SetProgress(float value, bool notify = true)
    {
        if (!HasServerAuthority)
            return;

        SetProgressInternal(value, notify);
    }

    public void SetEnergy(float value, bool notify = true)
    {
        if (!HasServerAuthority)
            return;

        SetEnergyInternal(value, notify);
    }

    public void SetGreening(float value, bool notify = true)
    {
        if (!HasServerAuthority)
            return;

        SetGreeningInternal(value, notify);
    }

    private bool SetProgressInternal(float value, bool notify)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentProgress, value))
            return false;

        currentProgress = value;
        RecalculateUnlocks();

        if (notify)
            OnProgressChanged?.Invoke(currentProgress);

        PushStateToNetwork();
        return true;
    }

    private bool SetEnergyInternal(float value, bool notify)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentEnergy, value))
            return false;

        currentEnergy = value;

        if (notify)
            OnEnergyChanged?.Invoke(currentEnergy);

        PushStateToNetwork();
        return true;
    }

    private bool SetGreeningInternal(float value, bool notify)
    {
        value = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(currentGreening, value))
            return false;

        currentGreening = value;

        if (notify)
            OnGreeningChanged?.Invoke(currentGreening);

        PushStateToNetwork();
        return true;
    }

    private void PushStateToNetwork()
    {
        if (!IsNetworkedSession || !HasServerAuthority)
            return;

        if (PlayerRegistry.I == null || !PlayerRegistry.I.IsSpawned)
            return;

        PlayerRegistry.I.ApplyResearchState(currentProgress, currentEnergy, currentGreening);
    }

    private void StartResearchLoopIfNeeded()
    {
        if (!HasServerAuthority || researchLoopCts != null)
            return;

        researchLoopCts = new CancellationTokenSource();
        ResearchTickLoop(researchLoopCts.Token).Forget();
    }

    private void StopResearchLoop()
    {
        if (researchLoopCts == null)
            return;

        researchLoopCts.Cancel();
        researchLoopCts.Dispose();
        researchLoopCts = null;
    }

    private void HandleSessionModeChanged(bool networked, bool serverAuthority)
    {
        if (!networked)
        {
            TeardownNetworkBindings();
            StartResearchLoopIfNeeded();
            return;
        }

        if (serverAuthority)
        {
            TeardownNetworkBindings();
            StartResearchLoopIfNeeded();
            PushStateToNetwork();
            return;
        }

        StopResearchLoop();
        TeardownNetworkBindings();
        BindNetworkStateAsync().Forget();
    }

    private async UniTaskVoid BindNetworkStateAsync()
    {
        if (!IsNetworkedSession || HasServerAuthority)
            return;

        await UniTask.WaitUntil(() => PlayerRegistry.I != null && PlayerRegistry.I.IsSpawned);
        if (this == null)
            return;

        if (networkBindingsActive && boundRegistry != null && boundRegistry.IsSpawned)
            return;

        TeardownNetworkBindings();
        boundRegistry = PlayerRegistry.I;
        if (boundRegistry == null)
            return;

        boundRegistry.ResearchProgressState.OnValueChanged += HandleNetworkProgressChanged;
        boundRegistry.ResearchEnergyState.OnValueChanged += HandleNetworkEnergyChanged;
        boundRegistry.ResearchGreeningState.OnValueChanged += HandleNetworkGreeningChanged;
        networkBindingsActive = true;

        ApplyNetworkState(boundRegistry.ResearchProgressState.Value, boundRegistry.ResearchEnergyState.Value, boundRegistry.ResearchGreeningState.Value);
    }

    private async UniTaskVoid WaitForBuildManagerReadyAsync()
    {
        await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null && BuildManager.I.ArcDB.IsLoaded);
        if (this == null)
            return;

        RecalculateUnlocks();
    }

    private void TeardownNetworkBindings()
    {
        if (!networkBindingsActive || boundRegistry == null)
            return;

        boundRegistry.ResearchProgressState.OnValueChanged -= HandleNetworkProgressChanged;
        boundRegistry.ResearchEnergyState.OnValueChanged -= HandleNetworkEnergyChanged;
        boundRegistry.ResearchGreeningState.OnValueChanged -= HandleNetworkGreeningChanged;
        networkBindingsActive = false;
        boundRegistry = null;
    }

    private void ApplyNetworkState(float progress, float energy, float greening)
    {
        SetProgressFromNetwork(progress);
        SetEnergyFromNetwork(energy);
        SetGreeningFromNetwork(greening);
    }

    private void HandleNetworkProgressChanged(float previous, float current)
    {
        SetProgressFromNetwork(current);
    }

    private void HandleNetworkEnergyChanged(float previous, float current)
    {
        SetEnergyFromNetwork(current);
    }

    private void HandleNetworkGreeningChanged(float previous, float current)
    {
        SetGreeningFromNetwork(current);
    }

    private void SetProgressFromNetwork(float value)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentProgress, value))
            return;

        currentProgress = value;
        RecalculateUnlocks();
        OnProgressChanged?.Invoke(currentProgress);
    }

    private void SetEnergyFromNetwork(float value)
    {
        value = Mathf.Max(0f, value);
        if (Mathf.Approximately(currentEnergy, value))
            return;

        currentEnergy = value;
        OnEnergyChanged?.Invoke(currentEnergy);
    }

    private void SetGreeningFromNetwork(float value)
    {
        value = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(currentGreening, value))
            return;

        currentGreening = value;
        OnGreeningChanged?.Invoke(currentGreening);
    }
}
