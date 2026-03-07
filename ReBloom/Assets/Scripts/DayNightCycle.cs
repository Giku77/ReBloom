using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.Netcode;

public class DayNightCycle : NetworkBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Environment Lighting (Runtime)")]
    [SerializeField] private bool useFlatAmbient = true;

    [SerializeField] private Color nightAmbientColor = new Color(0.03f, 0.04f, 0.06f, 1f);
    [SerializeField] private float nightAmbientIntensity = 1.2f;

    [SerializeField] private Color dayAmbientColor = new Color(0.75f, 0.78f, 0.82f, 1f);
    [SerializeField] private float dayAmbientIntensity = 1.0f;

    [SerializeField] private float dayReflectionIntensity = 1.0f;
    [SerializeField] private float nightReflectionIntensity = 1.25f;

    [SerializeField] private AnimationCurve dayFactorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Light")]
    public Light sun;
    public Light moon;

    [Header("Time Settings")]
    public float dayLengthInSeconds = 2160f; // 36분 = 게임 하루
    [Range(0f, 2160f)]
    [SerializeField] private float initialCurrentTime = 0f;
    [SerializeField] private int initialDay = 1;

    [Header("Network Sync")]
    [SerializeField] private float snapshotSyncInterval = 0.25f;

    [Header("Temperature Settings")]
    public float maxTempDelta = 8f;
    public float minTempDelta = -8f;
    public AnimationCurve temperatureCurve;

    [Header("Sun Settings")]
    public AnimationCurve sunAngleCurve;
    [SerializeField] private float maxSunIntensity = 3f;

    [Header("Clouds Setting")]
    [SerializeField] private Renderer cloudRenderer;
    [SerializeField] private float dayCloudAlpha = 1f;
    [SerializeField] private float nightCloudAlpha = 0.1f;
    [SerializeField] private float cloudLerpSpeed = 0.1f;

    [Header("Fog (RenderSettings)")]
    [SerializeField] private bool controlRenderFog = true;
    [SerializeField] private Color dayFogColor = new Color(0.78f, 0.72f, 0.45f, 1f);
    [SerializeField] private Color nightFogColor = new Color(0.06f, 0.07f, 0.09f, 1f);
    [SerializeField] private float dayFogDensity = 0.02f;
    [SerializeField] private float nightFogDensity = 0.04f;

    [Header("GI")]
    [SerializeField] private float giUpdateInterval = 1f;

    private Color nightSkyboxTint = new Color(0f, 0f, 0f);
    private float skyboxLerpSpeed = 0.5f;

    private Material skyboxMaterial;
    private Material originalSkyboxMaterial;
    private Color originalSkyboxTint;

    private float yEast = 90f;
    private float yWest = -90f;

    private int lastHour = -1;
    public float SunAngle { get; private set; }
    public float SunYRotation { get; private set; }
    public float TimeTempDelta { get; private set; }

    public int CurrentDay { get; private set; } = 1;
    public DayCycle CurrentDayCycle { get; private set; } = DayCycle.Day;
    public string CurrentDayName { get; private set; } = "낮";

    private Color originalEmissionColor;

    private bool _useGreeningFog;
    private Color _greeningFogColor = Color.white;
    private bool _disableFogByGreening;

    private float _nextGIUpdateTime;
    private float _snapshotSyncTimer;

    // -------------------------
    // 서버 권위 원본 상태
    // -------------------------
    private float _serverCurrentTime;
    private int _serverCurrentDay = 1;

    // -------------------------
    // 클라/공용 런타임 캐시
    // 이 값을 기준으로 렌더링/UI 계산
    // -------------------------
    private float _runtimeCurrentTime;
    private int _runtimeCurrentDay = 1;

    // -------------------------
    // 네트워크 스냅샷
    // 매 프레임 전송 안 하고 스냅샷만 동기화
    // -------------------------
    private readonly NetworkVariable<float> syncedCurrentTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> syncedCurrentDay = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<double> syncedServerTime = new(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool HasNetworkSession =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening &&
        IsSpawned;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (temperatureCurve == null || temperatureCurve.keys.Length == 0)
            InitializeTemperatureCurve();

        if (sunAngleCurve == null || sunAngleCurve.keys.Length == 0)
            InitializeSunCurve();

        if (cloudRenderer != null)
            originalEmissionColor = cloudRenderer.material.GetColor("_EmissionColor");

        // 오프라인/싱글 플레이 fallback 초기값
        _serverCurrentTime = Mathf.Clamp(initialCurrentTime, 0f, dayLengthInSeconds - 0.001f);
        _serverCurrentDay = Mathf.Max(1, initialDay);

        _runtimeCurrentTime = _serverCurrentTime;
        _runtimeCurrentDay = _serverCurrentDay;

        lastHour = GetCurrentHour();

        UpdateDayCycle();
        UpdateTemperature();
        UpdateSun();
        UpdateMoon();
        UpdateCloudAlpha();
        ApplyEnvironment();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _serverCurrentTime = Mathf.Clamp(initialCurrentTime, 0f, dayLengthInSeconds - 0.001f);
            _serverCurrentDay = Mathf.Max(1, initialDay);
            PushSnapshot(force: true);
        }

        RefreshRuntimeState();
        lastHour = GetCurrentHour();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (originalSkyboxMaterial != null)
            RenderSettings.skybox = originalSkyboxMaterial;

        if (skyboxMaterial != null)
            Destroy(skyboxMaterial);
    }

    private void Update()
    {
        if (HasNetworkSession)
        {
            if (IsServer)
            {
                TickServerTime();
                TrySyncSnapshot();
            }

            RefreshRuntimeState();
        }
        else
        {
            // 싱글/오프라인 fallback
            TickOfflineTime();
            _runtimeCurrentTime = _serverCurrentTime;
            _runtimeCurrentDay = _serverCurrentDay;
        }

        CurrentDay = _runtimeCurrentDay;

        UpdateDayCycle();
        UpdateTemperature();
        UpdateSun();
        UpdateMoon();
        UpdateCloudAlpha();
        ApplyEnvironment();

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            PrintDebugInfo();
    }

    // =========================================================
    // 서버 / 오프라인 시간 진행
    // =========================================================

    private void TickServerTime()
    {
        _serverCurrentTime += Time.deltaTime;

        bool dayChanged = false;

        while (_serverCurrentTime >= dayLengthInSeconds)
        {
            _serverCurrentTime -= dayLengthInSeconds;
            _serverCurrentDay++;
            dayChanged = true;
        }

        int currentHour = GetHourFromTime(_serverCurrentTime);

        if (lastHour != currentHour && currentHour == 0)
        {
            // 자정 진입 시 저장
            AutoSaveService.I?.RequestSave("DayChanged");
        }

        if (dayChanged)
        {
            AutoSaveService.I?.RequestSave("DayChanged");
        }

        lastHour = currentHour;
    }

    private void TickOfflineTime()
    {
        _serverCurrentTime += Time.deltaTime;

        bool dayChanged = false;

        while (_serverCurrentTime >= dayLengthInSeconds)
        {
            _serverCurrentTime -= dayLengthInSeconds;
            _serverCurrentDay++;
            dayChanged = true;
        }

        int currentHour = GetHourFromTime(_serverCurrentTime);

        if (lastHour != currentHour && currentHour == 0)
            AutoSaveService.I?.RequestSave("DayChanged");

        if (dayChanged)
            AutoSaveService.I?.RequestSave("DayChanged");

        lastHour = currentHour;
    }

    private void TrySyncSnapshot()
    {
        if (!IsServer) return;

        _snapshotSyncTimer += Time.deltaTime;
        if (_snapshotSyncTimer < snapshotSyncInterval) return;

        _snapshotSyncTimer = 0f;
        PushSnapshot(force: false);
    }

    private void PushSnapshot(bool force)
    {
        if (!IsServer) return;

        if (!force)
        {
            if (Mathf.Abs(syncedCurrentTime.Value - _serverCurrentTime) < 0.01f &&
                syncedCurrentDay.Value == _serverCurrentDay)
                return;
        }

        syncedCurrentTime.Value = _serverCurrentTime;
        syncedCurrentDay.Value = _serverCurrentDay;
        syncedServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    private void RefreshRuntimeState()
    {
        if (!HasNetworkSession)
        {
            _runtimeCurrentTime = _serverCurrentTime;
            _runtimeCurrentDay = _serverCurrentDay;
            return;
        }

        if (IsServer)
        {
            _runtimeCurrentTime = _serverCurrentTime;
            _runtimeCurrentDay = _serverCurrentDay;
            return;
        }

        float predictedTime = syncedCurrentTime.Value;
        int predictedDay = syncedCurrentDay.Value;

        double elapsed = NetworkManager.Singleton.ServerTime.Time - syncedServerTime.Value;
        if (elapsed < 0d) elapsed = 0d;

        predictedTime += (float)elapsed;

        while (predictedTime >= dayLengthInSeconds)
        {
            predictedTime -= dayLengthInSeconds;
            predictedDay++;
        }

        _runtimeCurrentTime = predictedTime;
        _runtimeCurrentDay = predictedDay;
    }

    // =========================================================
    // 공개 API (서버 권위)
    // =========================================================

    public void SetTime(int day, int hour, int minute)
    {
        if (!HasNetworkSession)
        {
            ApplySetTimeLocal(day, hour, minute);
            return;
        }

        if (IsServer)
            ApplySetTimeServer(day, hour, minute);
        else
            RequestSetTimeRpc(day, hour, minute);
    }

    public void AdvanceMinutes(float minutes)
    {
        if (!HasNetworkSession)
        {
            ApplyAddSecondsLocal(dayLengthInSeconds * (minutes / 1440f));
            return;
        }

        if (IsServer)
            ApplyAddSecondsServer(dayLengthInSeconds * (minutes / 1440f));
        else
            RequestAdvanceMinutesRpc(minutes);
    }

    public void AdvanceHours(float hours)
    {
        AdvanceMinutes(hours * 60f);
    }

    public void SleepUntilMorning()
    {
        int currentHour = GetCurrentHour();

        int hoursToAdvance;
        if (currentHour < 7) hoursToAdvance = 7 - currentHour;
        else hoursToAdvance = 24 - currentHour + 7;

        AdvanceHours(hoursToAdvance);
    }

    [Rpc(SendTo.Server)]
    private void RequestSetTimeRpc(int day, int hour, int minute, RpcParams rpcParams = default)
    {
        ApplySetTimeServer(day, hour, minute);
    }

    [Rpc(SendTo.Server)]
    private void RequestAdvanceMinutesRpc(float minutes, RpcParams rpcParams = default)
    {
        ApplyAddSecondsServer(dayLengthInSeconds * (minutes / 1440f));
    }

    private void ApplySetTimeServer(int day, int hour, int minute)
    {
        if (!IsServer) return;

        _serverCurrentDay = Mathf.Max(1, day);

        float totalMinutes = (hour * 60f + minute);
        totalMinutes = (totalMinutes - 300f + 1440f) % 1440f;
        _serverCurrentTime = (totalMinutes / 1440f) * dayLengthInSeconds;

        lastHour = GetHourFromTime(_serverCurrentTime);
        PushSnapshot(force: true);
    }

    private void ApplySetTimeLocal(int day, int hour, int minute)
    {
        _serverCurrentDay = Mathf.Max(1, day);

        float totalMinutes = (hour * 60f + minute);
        totalMinutes = (totalMinutes - 300f + 1440f) % 1440f;
        _serverCurrentTime = (totalMinutes / 1440f) * dayLengthInSeconds;

        _runtimeCurrentDay = _serverCurrentDay;
        _runtimeCurrentTime = _serverCurrentTime;

        lastHour = GetHourFromTime(_serverCurrentTime);
    }

    private void ApplyAddSecondsServer(float seconds)
    {
        if (!IsServer) return;

        _serverCurrentTime += seconds;
        bool dayChanged = false;

        while (_serverCurrentTime >= dayLengthInSeconds)
        {
            _serverCurrentTime -= dayLengthInSeconds;
            _serverCurrentDay++;
            dayChanged = true;
        }

        while (_serverCurrentTime < 0f)
        {
            _serverCurrentTime += dayLengthInSeconds;
            _serverCurrentDay = Mathf.Max(1, _serverCurrentDay - 1);
            dayChanged = true;
        }

        if (dayChanged)
            AutoSaveService.I?.RequestSave("DayChanged");

        lastHour = GetHourFromTime(_serverCurrentTime);
        PushSnapshot(force: true);
    }

    private void ApplyAddSecondsLocal(float seconds)
    {
        _serverCurrentTime += seconds;
        bool dayChanged = false;

        while (_serverCurrentTime >= dayLengthInSeconds)
        {
            _serverCurrentTime -= dayLengthInSeconds;
            _serverCurrentDay++;
            dayChanged = true;
        }

        while (_serverCurrentTime < 0f)
        {
            _serverCurrentTime += dayLengthInSeconds;
            _serverCurrentDay = Mathf.Max(1, _serverCurrentDay - 1);
            dayChanged = true;
        }

        if (dayChanged)
            AutoSaveService.I?.RequestSave("DayChanged");

        _runtimeCurrentTime = _serverCurrentTime;
        _runtimeCurrentDay = _serverCurrentDay;

        lastHour = GetHourFromTime(_serverCurrentTime);
    }

    // =========================================================
    // 시간 계산
    // =========================================================

    public int GetCurrentHour()
    {
        return GetHourFromTime(_runtimeCurrentTime);
    }

    public int GetCurrentMinute()
    {
        return GetMinuteFromTime(_runtimeCurrentTime);
    }

    private int GetHourFromTime(float timeValue)
    {
        float totalMinutes = (timeValue / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes / 60f);
    }

    private int GetMinuteFromTime(float timeValue)
    {
        float totalMinutes = (timeValue / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes % 60f);
    }

    private float GetDayFactorByHour()
    {
        int h = GetCurrentHour();
        int m = GetCurrentMinute();
        float hour = h + m / 60f;

        if (hour >= 5f && hour < 7f) return Mathf.InverseLerp(5f, 7f, hour);
        if (hour >= 7f && hour < 17f) return 1f;
        if (hour >= 17f && hour < 19f) return 1f - Mathf.InverseLerp(17f, 19f, hour);

        return 0f;
    }

    // =========================================================
    // 비주얼 / 환경 적용
    // =========================================================

    private void UpdateDayCycle()
    {
        int hour = GetCurrentHour();

        if (hour >= 5 && hour < 7)
        {
            CurrentDayCycle = DayCycle.Dawn;
            CurrentDayName = "일출";
        }
        else if (hour >= 7 && hour < 11)
        {
            CurrentDayCycle = DayCycle.Morning;
            CurrentDayName = "아침";
        }
        else if (hour >= 11 && hour < 17)
        {
            CurrentDayCycle = DayCycle.Day;
            CurrentDayName = "낮";
        }
        else if (hour >= 17 && hour < 19)
        {
            CurrentDayCycle = DayCycle.Dusk;
            CurrentDayName = "일몰";
        }
        else
        {
            CurrentDayCycle = DayCycle.Night;
            CurrentDayName = "밤";
        }
    }

    private void UpdateTemperature()
    {
        float t = _runtimeCurrentTime / dayLengthInSeconds;
        float delta = temperatureCurve.Evaluate(t);
        TimeTempDelta = Mathf.Lerp(minTempDelta, maxTempDelta, (delta + 1f) * 0.5f);
    }

    private void UpdateSun()
    {
        if (sun == null) return;

        float t = _runtimeCurrentTime / dayLengthInSeconds;

        SunAngle = sunAngleCurve.Evaluate(t);
        SunYRotation = Mathf.Lerp(yEast, yWest, t);

        sun.transform.rotation = Quaternion.Euler(SunAngle, SunYRotation, 0f);

        float normalized = Mathf.Clamp01(SunAngle / 90f);
        float targetIntensity = Mathf.Lerp(0f, maxSunIntensity, normalized);

        sun.intensity = Mathf.Lerp(sun.intensity, targetIntensity, 2f * Time.deltaTime);
    }

    private void UpdateMoon()
    {
        if (moon == null) return;

        if (CurrentDayCycle == DayCycle.Dusk)
        {
            int hour = GetCurrentHour();
            int minute = GetCurrentMinute();
            float fadeT = ((hour - 17) * 60 + minute) / 120f;
            moon.intensity = Mathf.Lerp(0f, 0.15f, fadeT);
            moon.enabled = true;
        }
        else if (CurrentDayCycle == DayCycle.Night)
        {
            moon.intensity = 0.15f;
            moon.enabled = true;
        }
        else
        {
            moon.intensity = 0f;
            moon.enabled = false;
        }
    }

    private void UpdateCloudAlpha()
    {
        if (cloudRenderer == null) return;

        float targetAlpha = _runtimeCurrentTime > 1400f ? nightCloudAlpha : dayCloudAlpha;

        Color c = cloudRenderer.material.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, cloudLerpSpeed * Time.deltaTime);
        cloudRenderer.material.color = c;

        float targetIntensity = _runtimeCurrentTime > 1400f ? 0f : 1f;
        Color currentEmission = cloudRenderer.material.GetColor("_EmissionColor");
        Color newEmission = Color.Lerp(
            currentEmission,
            originalEmissionColor * targetIntensity,
            cloudLerpSpeed * Time.deltaTime);

        cloudRenderer.material.SetColor("_EmissionColor", newEmission);
    }

    private void ApplyEnvironment()
    {
        float t = _runtimeCurrentTime / dayLengthInSeconds;
        float dayFactor = GetDayFactorByHour();
        float nightFloor = 0.08f;
        dayFactor = Mathf.Lerp(nightFloor, 1f, dayFactor);

        RenderSettings.sun = sun;

        if (useFlatAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, dayFactor);
            RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, dayFactor);
        }
        else
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, dayFactor);
        }

        RenderSettings.reflectionIntensity =
            Mathf.Lerp(nightReflectionIntensity, dayReflectionIntensity, dayFactor);

        if (Time.time >= _nextGIUpdateTime)
        {
            _nextGIUpdateTime = Time.time + giUpdateInterval;
            DynamicGI.UpdateEnvironment();
        }

        if (controlRenderFog)
        {
            Color baseFog = Color.Lerp(nightFogColor, dayFogColor, dayFactor);
            float baseDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, dayFactor);

            if (_disableFogByGreening)
            {
                RenderSettings.fog = false;
            }
            else
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;

                Color finalFog = _useGreeningFog ? (baseFog * _greeningFogColor) : baseFog;

                RenderSettings.fogColor = finalFog;
                RenderSettings.fogDensity = baseDensity;
            }
        }
    }

    private void PrintDebugInfo()
    {
        Debug.Log($"========== Day {CurrentDay} - {GetCurrentHour():D2}:{GetCurrentMinute():D2} ({CurrentDayName}) ==========");
        Debug.Log($"Runtime Time (seconds): {_runtimeCurrentTime:F1}s");
        Debug.Log($"Server Time (seconds): {_serverCurrentTime:F1}s");
        Debug.Log($"Time Temp Delta: {TimeTempDelta:F1}°C");
        Debug.Log($"Sun Angle (X): {SunAngle:F1}°");
        Debug.Log($"Sun Y Rotation: {SunYRotation:F1}°");
        Debug.Log($"Sun Intensity: {(sun != null ? sun.intensity : 0f):F2}");
        Debug.Log($"Sun Enabled: {(sun != null && sun.enabled)}");
        Debug.Log($"IsServer={IsServer}, IsClient={IsClient}, IsSpawned={IsSpawned}");
        Debug.Log("=============================================");
    }

    private void InitializeTemperatureCurve()
    {
        temperatureCurve = new AnimationCurve();

        temperatureCurve.AddKey(0f, -0.8f);
        temperatureCurve.AddKey(0.042f, -1.0f);
        temperatureCurve.AddKey(0.417f, 1.0f);
        temperatureCurve.AddKey(1f, -0.8f);

        for (int i = 0; i < temperatureCurve.keys.Length; i++)
            temperatureCurve.SmoothTangents(i, 0.5f);
    }

    private void InitializeSunCurve()
    {
        sunAngleCurve = new AnimationCurve();

        sunAngleCurve.AddKey(0f, 0f);
        sunAngleCurve.AddKey(0.0926f, 30f);
        sunAngleCurve.AddKey(0.3241f, 90f);
        sunAngleCurve.AddKey(0.5556f, 30f);
        sunAngleCurve.AddKey(0.6019f, 15f);
        sunAngleCurve.AddKey(0.6481f, 0f);
        sunAngleCurve.AddKey(0.8241f, -90f);
        sunAngleCurve.AddKey(1f, 0f);

        for (int i = 0; i < sunAngleCurve.keys.Length; i++)
            sunAngleCurve.SmoothTangents(i, 0.5f);
    }

    public bool IsNightTime()
    {
        return CurrentDayCycle == DayCycle.Night;
    }

    public void SetGreeningFog(Color greeningFogColor01, bool disableFog)
    {
        _useGreeningFog = true;
        _greeningFogColor = greeningFogColor01;
        _disableFogByGreening = disableFog;
    }

    public void ClearGreeningFog()
    {
        _useGreeningFog = false;
        _greeningFogColor = Color.white;
        _disableFogByGreening = false;
    }
}