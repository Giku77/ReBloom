using UnityEngine;
using UnityEngine.InputSystem;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }
    

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

    

    private float yEast = 90f;
    private float yWest = -90f;

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
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        
        if (currentTime >= dayLengthInSeconds)
        {
            currentTime -= dayLengthInSeconds;
            CurrentDay++;
        }

        UpdateDayCycle();
        UpdateTemperature();
        UpdateSun();
        UpdateMoon();
        UpdateCloudAlpha();

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
}

