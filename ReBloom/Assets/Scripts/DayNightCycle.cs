using UnityEngine;
using UnityEngine.InputSystem;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }
    

    [Header("Light")]
    public Light sun;

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

    private float yEast = 90f;
    private float yWest = -90f;

    public float SunAngle { get; private set; }
    public float SunYRotation { get; private set; }
    public float TimeTempDelta { get; private set; }
    public int CurrentDay { get; private set; } = 1;

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
    
    void Start()
    {
        if (temperatureCurve == null || temperatureCurve.keys.Length == 0)
            InitializeTemperatureCurve();

        if (sunAngleCurve == null || sunAngleCurve.keys.Length == 0)
            InitializeSunCurve();
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        
        if (currentTime >= dayLengthInSeconds)
        {
            currentTime -= dayLengthInSeconds;
            CurrentDay++;
        }

        UpdateTemperature();
        UpdateSun();

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PrintDebugInfo();
        }
    }

void UpdateTemperature()
    {
        float t = currentTime / dayLengthInSeconds;
        float delta = temperatureCurve.Evaluate(t);

        TimeTempDelta = Mathf.Lerp(minTempDelta, maxTempDelta, (delta + 1f) * 0.5f);
    }

    void UpdateSun()
    {
        if (sun == null) return;

        float t = currentTime / dayLengthInSeconds;

        SunAngle = sunAngleCurve.Evaluate(t);

        SunYRotation = Mathf.Lerp(yEast, yWest, t);

        sun.transform.rotation = Quaternion.Euler(SunAngle, SunYRotation, 0f);

        sun.intensity = SunAngle > 0 ? 1f : 0.1f;
    }

    public int GetCurrentHour()
    {
        float totalMinutes = (currentTime / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes / 60f);
    }

    string GetTimeOfDayString()
    {
        int hour = GetCurrentHour();
        
        if (hour >= 5 && hour < 7)
            return "일출";
        else if (hour >= 7 && hour < 11)
            return "아침";
        else if (hour >= 11 && hour < 17)
            return "낮";
        else if (hour >= 17 && hour < 19)
            return "일몫";
        else
            return "밤";
    }
    
public int GetCurrentMinute()
    {
        float totalMinutes = (currentTime / dayLengthInSeconds) * 1440f + 300f;
        totalMinutes %= 1440f;
        return Mathf.FloorToInt(totalMinutes % 60f);
    }

void PrintDebugInfo()
    {
        string timeOfDay = GetTimeOfDayString();
        Debug.Log($"========== Day {CurrentDay} - {GetCurrentHour():D2}:{GetCurrentMinute():D2} ({timeOfDay}) ==========");
        Debug.Log($"Current Time (seconds): {currentTime:F1}s");
        Debug.Log($"Time Temp Delta: {TimeTempDelta:F1}°C");
        Debug.Log($"Sun Angle (X): {SunAngle:F1}°");
        Debug.Log($"Sun Y Rotation: {SunYRotation:F1}°");
        Debug.Log($"Sun Intensity: {sun.intensity:F2}");
        Debug.Log($"Sun Enabled: {sun.enabled}");
        Debug.Log($"=============================================");
    }

void InitializeTemperatureCurve()
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

void InitializeSunCurve()
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
}
