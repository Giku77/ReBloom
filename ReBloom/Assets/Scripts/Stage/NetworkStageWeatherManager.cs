using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkStageWeatherManager : NetworkBehaviour
{
    public static NetworkStageWeatherManager I { get; private set; }

    private StageDB stageDB;

    public NetworkList<StageWeatherState> states;

    // 서버 전용: 빠른 조회용 인덱스 캐시
    private readonly Dictionary<int, int> stageIdToIndex = new();

    private void Awake()
    {
        I = this;
        states = new NetworkList<StageWeatherState>();
    }

    public override void OnDestroy()
    {
        if (I == this) I = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            stageDB = new StageDB();
            stageDB.LoadFromBG();

            double now = NetworkManager.Singleton.ServerTime.Time;
            InitAllStagesServer(now);
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton == null) return;

        double now = NetworkManager.Singleton.ServerTime.Time;

        // 서버에서만 각 스테이지 "만료 검사"
        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];

            // startServerTime이 0이면(초기화 누락 등) 지금으로 보정
            if (s.startServerTime <= 0d)
                s.startServerTime = now;

            double elapsed = now - s.startServerTime;

            // duration 안전장치
            if (float.IsNaN(s.duration) || s.duration < 1f)
                s.duration = 1f;

            if (elapsed >= s.duration)
            {
                SetRandomWeatherServer(ref s, now);
                states[i] = s; 
            }
        }
    }

    private void InitAllStagesServer(double now)
    {
        states.Clear();
        stageIdToIndex.Clear();

        foreach (var kv in stageDB.GetAll())
        {
            int stageId = kv.Key;
            var data = kv.Value;

            var s = new StageWeatherState
            {
                stageId = stageId,
                weather = PickRandomWeather(data),

                startServerTime = now,
                duration = 60f,

                pollution = 0f,
                thirst = 0f,
                temp = 0f
            };

            ApplyWeatherParams(data, ref s);

            // duration NaN/최소값 방어
            if (float.IsNaN(s.duration) || s.duration < 1f) s.duration = 1f;

            stageIdToIndex[stageId] = states.Count;
            states.Add(s);
        }
    }

    public bool TryGet(int stageId, out StageWeatherState state)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i].stageId == stageId)
            {
                state = states[i];
                return true;
            }
        }

        state = default;
        return false;
    }

    public bool TryGetTimer(int stageId, out float timer, out float duration, out StageWeatherState state)
    {
        timer = 0f;
        duration = 1f;
        state = default;

        if (!TryGet(stageId, out state)) return false;

        duration = Mathf.Max(1f, state.duration);

        if (NetworkManager.Singleton == null)
            return true;

        double now = NetworkManager.Singleton.ServerTime.Time;
        float t = (float)(now - state.startServerTime);
        timer = Mathf.Clamp(t, 0f, duration);
        return true;
    }

    // 서버 전용: 강제 날씨 지정(필요하면)
    public void SetWeatherServer(int stageId, WeatherType weather)
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton == null) return;
        if (!stageDB.TryGet(stageId, out var data)) return;

        if (!stageIdToIndex.TryGetValue(stageId, out int idx))
        {
            idx = FindIndex(stageId);
            if (idx < 0) return;
            stageIdToIndex[stageId] = idx;
        }

        double now = NetworkManager.Singleton.ServerTime.Time;

        var s = states[idx];
        s.weather = weather;
        ApplyWeatherParams(data, ref s);

        s.startServerTime = now;

        if (float.IsNaN(s.duration) || s.duration < 1f) s.duration = 1f;

        states[idx] = s;
    }

    private int FindIndex(int stageId)
    {
        for (int i = 0; i < states.Count; i++)
            if (states[i].stageId == stageId) return i;
        return -1;
    }

    private void SetRandomWeatherServer(ref StageWeatherState s, double now)
    {
        if (!stageDB.TryGet(s.stageId, out var data)) return;

        s.weather = PickRandomWeather(data);
        ApplyWeatherParams(data, ref s);

        s.startServerTime = now;

        if (float.IsNaN(s.duration) || s.duration < 1f) s.duration = 1f;
    }

    private WeatherType PickRandomWeather(StageData data)
    {
        float totalRate =
            data.sunnyRate + data.rainRate + data.radioRate +
            data.snowRate + data.thunderRate + data.hotRate;

        float r = Random.Range(0f, totalRate);
        float acc = 0f;

        acc += data.sunnyRate; if (r < acc) return WeatherType.Sunny;
        acc += data.rainRate; if (r < acc) return WeatherType.Rain;
        acc += data.radioRate; if (r < acc) return WeatherType.Radio;
        acc += data.snowRate; if (r < acc) return WeatherType.Snow;
        acc += data.thunderRate; if (r < acc) return WeatherType.Thunder;
        return WeatherType.Hot;
    }

    private void ApplyWeatherParams(StageData data, ref StageWeatherState s)
    {
        switch (s.weather)
        {
            case WeatherType.Sunny:
                s.duration = data.sunny_d + Random.Range(-data.sunny_vari, data.sunny_vari);
                s.pollution = data.sunnyPollution;
                s.thirst = data.sunnyThirst;
                s.temp = data.sunnyTemp;
                break;

            case WeatherType.Rain:
                s.duration = data.rain_d + Random.Range(-data.rain_vari, data.rain_vari);
                s.pollution = data.rainPollution;
                s.thirst = data.rainThirst;
                s.temp = data.rainTemp;
                break;

            case WeatherType.Radio:
                s.duration = data.radio_d + Random.Range(-data.radio_vari, data.radio_vari);
                s.pollution = data.radioPollution;
                s.thirst = data.radioThirst;
                s.temp = data.radioTemp;
                break;

            case WeatherType.Snow:
                s.duration = data.snow_d + Random.Range(-data.snow_vari, data.snow_vari);
                s.pollution = data.snowPollution;
                s.thirst = data.snowThirst;
                s.temp = data.snowTemp;
                break;

            case WeatherType.Thunder:
                s.duration = data.thunde_d + Random.Range(-data.thunde_vari, data.thunde_vari);
                s.pollution = data.thundePollution;
                s.thirst = data.thundeThirst;
                s.temp = data.thundeTemp;
                break;

            case WeatherType.Hot:
                s.duration = data.hot_d + Random.Range(-data.hot_vari, data.hot_vari);
                s.pollution = data.hotPollution;
                s.thirst = data.hotThirst;
                s.temp = data.hotTemp;
                break;
        }

        if (float.IsNaN(s.duration) || s.duration < 1f) s.duration = 1f;
    }
}