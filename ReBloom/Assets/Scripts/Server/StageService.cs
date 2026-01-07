using System;
using UnityEngine;

public class StageService : MonoBehaviour
{
    public static StageService I { get; private set; }

    public StageBase CurrentStage { get; private set; }
    public StageBase PreviousStage { get; private set; }

    public bool IsInside { get; private set; }
    public bool CanBuild { get; private set; } = true;

    public event Action<StageBase, StageBase> OnStageChanged; // (prev, cur)
    public event Action<bool> OnInsideChanged;
    public event Action<bool> OnCanBuildChanged;

    [Header("Weather FX")]
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject snowEffect;
    [SerializeField] private GameObject radioEffect;
    [SerializeField] private GameObject thunderEffect;

    private void Awake()
    {
        I = this;
    }

    public void SetStage(StageBase stage)
    {
        if (stage == null) return;

        bool changed = (CurrentStage == null || CurrentStage.StageID != stage.StageID);
        if (!changed)
        {
            // 같은 스테이지면 날씨만 최신화할 수도 있음 (원하면)
            ApplyWeather(stage.CurrentWeather);
            return;
        }

        PreviousStage = CurrentStage ?? stage;
        CurrentStage = stage;

        ApplyWeather(stage.CurrentWeather);
        OnStageChanged?.Invoke(PreviousStage, CurrentStage);
    }

    public void SetInside(bool inside)
    {
        if (IsInside == inside) return;
        IsInside = inside;

        if (inside) ClearWeatherEffect();
        else if (CurrentStage != null) ApplyWeather(CurrentStage.CurrentWeather);

        OnInsideChanged?.Invoke(IsInside);
    }

    public void SetCanBuild(bool canBuild)
    {
        if (CanBuild == canBuild) return;
        CanBuild = canBuild;
        OnCanBuildChanged?.Invoke(CanBuild);
    }

    public float GetCurrentPollutionMultiplier()
    {
        if (CurrentStage?.Data == null) return 0f;
        return CurrentStage.Data.stagePollution + CurrentStage.CurrentPollution;
    }

    public float GetCurrentThirst()
    {
        if (CurrentStage?.Data == null) return 0f;
        return CurrentStage.CurrentThirst;
    }

    private void ApplyWeather(WeatherType weather)
    {
        thunderEffect?.SetActive(false);
        rainEffect?.SetActive(false);
        snowEffect?.SetActive(false);
        radioEffect?.SetActive(false);

        switch (weather)
        {
            case WeatherType.Rain: rainEffect?.SetActive(true); break;
            case WeatherType.Snow: snowEffect?.SetActive(true); break;
            case WeatherType.Radio: radioEffect?.SetActive(true); break;
            case WeatherType.Thunder: rainEffect?.SetActive(true); thunderEffect?.SetActive(true); break;
        }
    }

    private void ClearWeatherEffect()
    {
        thunderEffect?.SetActive(false);
        rainEffect?.SetActive(false);
        snowEffect?.SetActive(false);
        radioEffect?.SetActive(false);
    }

    public void ForceSetStage(int stageId)
    {
        var all = FindObjectsByType<StageBase>(FindObjectsSortMode.None);

        foreach (var s in all)
        {
            if (s != null && s.StageID == stageId)
            {
                // SetStage가 내부에서 changed 검사 + 이벤트/FX 처리까지 해줌
                SetStage(s);
                return;
            }
        }

        Debug.LogWarning($"[StageService] ForceSetStage 실패: StageID={stageId} StageBase를 찾지 못함");
    }

}
