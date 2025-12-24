using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Environment Lighting (Runtime)")]
    [SerializeField] private bool useFlatAmbient = true;

    // 밤에도 외관 안 죽게 하는 "채움광"
    [SerializeField] private Color nightAmbientColor = new Color(0.03f, 0.04f, 0.06f, 1f);
    [SerializeField] private float nightAmbientIntensity = 1.2f;

    [SerializeField] private Color dayAmbientColor = new Color(0.75f, 0.78f, 0.82f, 1f);
    [SerializeField] private float dayAmbientIntensity = 1.0f;

    // 반사(재질이 죽어보이는 거 방지)
    [SerializeField] private float dayReflectionIntensity = 1.0f;
    [SerializeField] private float nightReflectionIntensity = 1.25f;

    // 밤/낮 전환용(0=밤, 1=낮)
    [SerializeField] private AnimationCurve dayFactorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Light")]
    public Light sun;
    public Light moon;

    [Header("Time Settings")]
    public float dayLengthInSeconds = 2160f; // 36분 기준 하루
    [Range(0f, 2160f)]
    public float currentTime = 0f;

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

    //[Header("Fog (RenderSettings)")]
    //[SerializeField] private bool controlRenderFog = true;

    //[SerializeField] private Color dayFogColor = new Color(0.78f, 0.72f, 0.45f, 1f); // 황사 느낌
    //[SerializeField] private Color nightFogColor = new Color(0.06f, 0.07f, 0.09f, 1f); // 밤엔 어두운 회청색(또는 누런색 더 어둡게)

    //[SerializeField] private float dayFogDensity = 0.02f;
    //[SerializeField] private float nightFogDensity = 0.04f; // 밤엔 조금 더 짙게 하면 “어두워 보이는” 효과 나기도 함

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
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

        originalEmissionColor = cloudRenderer.material.GetColor("_EmissionColor");

        //originalSkyboxMaterial = RenderSettings.skybox;
        //if (originalSkyboxMaterial != null)
        //{
        //    skyboxMaterial = new Material(originalSkyboxMaterial);
        //    RenderSettings.skybox = skyboxMaterial;

        //    if (skyboxMaterial.HasProperty("_Tint"))
        //    {
        //        originalSkyboxTint = skyboxMaterial.GetColor("_Tint");
        //    }
        //}
    }

    private void OnDestroy()
    {
        if (originalSkyboxMaterial != null)
        {
            RenderSettings.skybox = originalSkyboxMaterial;
        }

        if (skyboxMaterial != null)
        {
            Destroy(skyboxMaterial);
        }
    }

    private float GetDayFactorByHour()
    {
        int h = GetCurrentHour();
        int m = GetCurrentMinute();
        float hour = h + m / 60f;

        // 05~07: 0 -> 1
        if (hour >= 5f && hour < 7f) return Mathf.InverseLerp(5f, 7f, hour);

        // 07~17: 1
        if (hour >= 7f && hour < 17f) return 1f;

        // 17~19: 1 -> 0
        if (hour >= 17f && hour < 19f) return 1f - Mathf.InverseLerp(17f, 19f, hour);

        // 나머지: 0
        return 0f;
    }


    private void Update()
    {
        currentTime += Time.deltaTime;
        
        //if (currentTime >= dayLengthInSeconds)
        //{
        //    currentTime -= dayLengthInSeconds;
        //    CurrentDay++;
        //}

        if (currentTime >= dayLengthInSeconds)
        {
            currentTime -= dayLengthInSeconds;
        }

        int currentHour = GetCurrentHour();

        // 🔥 자정(00시) 진입 순간에 날짜 증가
        if (lastHour != currentHour && currentHour == 0)
        {
            CurrentDay++;
        }

        lastHour = currentHour;

        UpdateDayCycle();
        UpdateTemperature();
        UpdateSun();
        UpdateMoon();
        UpdateCloudAlpha();
        ApplyEnvironment();

        //아포칼립스 스카이박스일때만 적용
        //UpdateSkybox();

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintDebugInfo();
        }
    }

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
        float t = currentTime / dayLengthInSeconds;
        float delta = temperatureCurve.Evaluate(t);

        TimeTempDelta = Mathf.Lerp(minTempDelta, maxTempDelta, (delta + 1f) * 0.5f);
    }

    private void UpdateSun()
    {
        if (sun == null) return;

        float t = currentTime / dayLengthInSeconds;

        SunAngle = sunAngleCurve.Evaluate(t);

        SunYRotation = Mathf.Lerp(yEast, yWest, t);

        sun.transform.rotation = Quaternion.Euler(SunAngle, SunYRotation, 0f);

        //float targetIntensity = Mathf.Clamp01(SunAngle / 90f);
        float normalized = Mathf.Clamp01(SunAngle / 90f);
        float targetIntensity = Mathf.Lerp(0f, maxSunIntensity, normalized);

        sun.intensity = Mathf.Lerp(sun.intensity, targetIntensity, 2f * Time.deltaTime);
    }

    public int GetCurrentHour()
    {
        float totalMinutes = (currentTime / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes / 60f);
    }
    
    public int GetCurrentMinute()
    {
        float totalMinutes = (currentTime / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes % 60f);
    }


    [SerializeField] private float giUpdateInterval = 1f;
    private float _nextGIUpdateTime;
    private void ApplyEnvironment()
    {
        float t = currentTime / dayLengthInSeconds;
        //float dayFactor = dayFactorCurve.Evaluate(t);
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

        RenderSettings.fog = dayFactor > 0.2f;

        //if (controlRenderFog)
        //{
        //    RenderSettings.fog = true;
        //    RenderSettings.fogMode = FogMode.Exponential;

        //    RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, dayFactor);
        //    RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, dayFactor);
        //}
    }

    private void PrintDebugInfo()
    {
        Debug.Log($"========== Day {CurrentDay} - {GetCurrentHour():D2}:{GetCurrentMinute():D2} ({CurrentDayName}) ==========");
        Debug.Log($"Current Time (seconds): {currentTime:F1}s");
        Debug.Log($"Time Temp Delta: {TimeTempDelta:F1}°C");
        Debug.Log($"Sun Angle (X): {SunAngle:F1}°");
        Debug.Log($"Sun Y Rotation: {SunYRotation:F1}°");
        Debug.Log($"Sun Intensity: {sun.intensity:F2}");
        Debug.Log($"Sun Enabled: {sun.enabled}");
        Debug.Log($"=============================================");
    }

    private void InitializeTemperatureCurve()
    {
        temperatureCurve = new AnimationCurve();
        
        // 기획서 기준: 최저 06시, 최고 14시
        temperatureCurve.AddKey(0f, -0.8f);      // 05:00 (최저 근처)
        temperatureCurve.AddKey(0.042f, -1.0f);  // 06:00 (최저점 -8도)
        temperatureCurve.AddKey(0.417f, 1.0f);   // 14:00 (최고점 +8도)
        temperatureCurve.AddKey(1f, -0.8f);      // 다음날 05:00 (최저 근처)
        
        for (int i = 0; i < temperatureCurve.keys.Length; i++)
            temperatureCurve.SmoothTangents(i, 0.5f);
    }

    private void InitializeSunCurve()
    {
        sunAngleCurve = new AnimationCurve();
        
        // 전체 24시간 (0~2160초)을 0~1로 매핑
        sunAngleCurve.AddKey(0f, 0f);                 // 05:00 (0도)
        sunAngleCurve.AddKey(0.0926f, 30f);           // 07:00 (30도)
        sunAngleCurve.AddKey(0.3241f, 90f);           // 12:00 (90도)
        sunAngleCurve.AddKey(0.5556f, 30f);           // 17:00 (30도)
        sunAngleCurve.AddKey(0.6019f, 15f);           // 18:00 (15도)
        sunAngleCurve.AddKey(0.6481f, 0f);            // 19:00 (0도)
        
        // 밤 시간: 태양이 지평선 아래로 한 바퀵
        sunAngleCurve.AddKey(0.8241f, -90f);          // 00:00 (최저점)
        sunAngleCurve.AddKey(1f, 0f);                 // 05:00 (0도, 다시 일출)
        
        for (int i = 0; i < sunAngleCurve.keys.Length; i++)
            sunAngleCurve.SmoothTangents(i, 0.5f);
    }

    private void UpdateMoon()
    {
        if (moon == null) return;

        // 일몰(17~19시) 동안 서서히 페이드 인
        if (CurrentDayCycle == DayCycle.Dusk)
        {
            int hour = GetCurrentHour();
            int minute = GetCurrentMinute();
            float fadeT = ((hour - 17) * 60 + minute) / 120f; // 0~1 (2시간 = 120분)
            moon.intensity = Mathf.Lerp(0f, 0.15f, fadeT);
            moon.enabled = true;
        }
        // 밤(19~05시) 동안 완전히 켜짐
        else if (CurrentDayCycle == DayCycle.Night)
        {
            moon.intensity = 0.15f;
            moon.enabled = true;
        }
        // 낮/일출/아침 동안 꺼짐
        else
        {
            moon.intensity = 0f;
            moon.enabled = false;
        }
    }

    private void UpdateCloudAlpha()
    {
        if (cloudRenderer == null) return;

        float targetAlpha = currentTime > 1400 ? nightCloudAlpha : dayCloudAlpha;
        Color c = cloudRenderer.material.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, cloudLerpSpeed * Time.deltaTime);
        cloudRenderer.material.color = c;

        float targetIntensity = currentTime > 1400 ? 0f : 1f;

        Color currentEmission = cloudRenderer.material.GetColor("_EmissionColor");

        Color newEmission = Color.Lerp(currentEmission, originalEmissionColor * targetIntensity, cloudLerpSpeed * Time.deltaTime);

        cloudRenderer.material.SetColor("_EmissionColor", newEmission);
    }

    public void AdvanceMinutes(float minutes)
    {
        // 24시간 = 1440분
        float addSeconds = dayLengthInSeconds * (minutes / 1440f);
        AddSeconds(addSeconds);
    }

    public void AdvanceHours(float hours)
    {
        AdvanceMinutes(hours * 60f);
    }

    private void AddSeconds(float seconds)
    {
        currentTime += seconds;

        // dayLengthInSeconds 넘어가면 날짜 증가 + 래핑
        while (currentTime >= dayLengthInSeconds)
        {
            currentTime -= dayLengthInSeconds;
            CurrentDay++;
        }
        while (currentTime < 0f)
        {
            currentTime += dayLengthInSeconds;
            CurrentDay = Mathf.Max(1, CurrentDay - 1);
        }

        UpdateDayCycle();
        UpdateTemperature();
        UpdateSun();
        UpdateMoon();
        UpdateCloudAlpha();
    }

    public bool IsNightTime()
    {
        return CurrentDayCycle == DayCycle.Night;
    }

    public void SleepUntilMorning()
    {
        int currentHour = GetCurrentHour();

        int hoursToAdvance;

        if (currentHour < 7)
        {
            hoursToAdvance = 7 - currentHour;
        }
        else
        {
            hoursToAdvance = 24 - currentHour + 7;
        }

        AdvanceHours(hoursToAdvance);
    }

    //private void UpdateSkybox()
    //{
    //    if (skyboxMaterial == null) return;

    //    float t = currentTime / dayLengthInSeconds;

    //    float nightFactor = 0f;

    //    if (t < 0.0926f)
    //    {
    //        nightFactor = Mathf.Lerp(1f, 0f, t / 0.0926f);
    //    }
    //    else if (t >= 0.0926f && t < 0.556f)
    //    {
    //        nightFactor = 0f;
    //    }
    //    else if (t >= 0.556f && t < 0.648f)
    //    {
    //        nightFactor = Mathf.Lerp(0f, 1f, (t - 0.556f) / 0.092f);
    //    }
    //    else
    //    {
    //        nightFactor = 1f;
    //    }

    //    Color targetTint = Color.Lerp(originalSkyboxTint, nightSkyboxTint, nightFactor);
    //    Color currentTint = skyboxMaterial.GetColor("_Tint");
    //    Color newTint = Color.Lerp(currentTint, targetTint, skyboxLerpSpeed * Time.deltaTime);
    //    skyboxMaterial.SetColor("_Tint", newTint);
    //}
}

