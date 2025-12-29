using System;
using UnityEngine;

[Serializable]
public class SettingManager : MonoBehaviour
{
    public static SettingManager I { get; private set; }

    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 0.5f;
    public float SFXVolume { get; private set; } = 1f;

    public Resolution CurrentResolution { get; private set; }
    public bool IsFullScreen { get; private set; } = true;
    public bool IsVSyncEnabled { get; private set; } = true;
    public float MouseSensitivity { get; private set; } = 3f;
    public int GraphicsQuality { get; private set; } = 0;
    public int TargetFrameRate { get; private set; } = 120; // -1=무제한
    public int PoppyVoiceType { get; private set; } = 1;

    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBGMVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<Resolution> OnResolutionChanged;
    public event Action<bool> OnFullScreenChanged;
    public event Action<bool> OnVSyncChanged;
    public event Action<float> OnMouseSensitivityChanged;
    public event Action<int> OnGraphicsQualityChanged;
    public event Action<int> OnTargetFrameRateChanged;
    public event Action<int> OnPoppyVoiceTypeChanged;

    [SerializeField]
    private Resolution windowedResolution = new Resolution
    {
        width = 1600,
        height = 900
    };

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);



        CurrentResolution = Screen.currentResolution;
        IsFullScreen = true;
        IsVSyncEnabled = QualitySettings.vSyncCount > 0;
        SetGraphicsQuality(GraphicsQuality);
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            TargetFrameRate = 30;
        }
        SetTargetFrameRate(TargetFrameRate);
        //GraphicsQuality = QualitySettings.GetQualityLevel();
        //int currentFPS = Application.targetFrameRate;
        //TargetFrameRate = currentFPS <= 0 ? -1 : currentFPS;

    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        OnMasterVolumeChanged?.Invoke(MasterVolume);
    }

    public void SetBGMVolume(float value)
    {
        BGMVolume = Mathf.Clamp01(value);
        OnBGMVolumeChanged?.Invoke(BGMVolume);
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        OnSFXVolumeChanged?.Invoke(SFXVolume);
    }

    public void SetResolution(Resolution resolution)
    {
        CurrentResolution = resolution;
        Screen.SetResolution(resolution.width, resolution.height, IsFullScreen);
        OnResolutionChanged?.Invoke(resolution);
    }

    //public void SetFullScreen(bool isFullScreen)
    //{
    //    IsFullScreen = isFullScreen;
    //    Screen.fullScreen = isFullScreen;
    //    Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, isFullScreen);
    //    OnFullScreenChanged?.Invoke(isFullScreen);
    //}

    public void SetFullScreen(bool isFullScreen)
    {
        IsFullScreen = isFullScreen;

        if (isFullScreen)
        {
            Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, FullScreenMode.ExclusiveFullScreen);
        }
        else
        {
            Screen.SetResolution(windowedResolution.width, windowedResolution.height, FullScreenMode.Windowed);
        }

        OnFullScreenChanged?.Invoke(isFullScreen);
    }

    public void SetVSync(bool enabled)
    {
        IsVSyncEnabled = enabled;
        // VSync: 0 = 끔, 1 = 60fps 제한, 2 = 30fps 제한 
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        OnVSyncChanged?.Invoke(enabled);
    }

    public void SetGraphicsQuality(int level)
    {
        //0 = Default, 1 = Mobile, 2 = Low, 3 = Medium, 4 = High, 5 = Ultra
        GraphicsQuality = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(GraphicsQuality);
        OnGraphicsQualityChanged?.Invoke(GraphicsQuality);
    }

    public void SetTargetFrameRate(int fps)
    {
        // -1 = 무제한, 30, 60, 120, 144 등
        TargetFrameRate = fps;
        Application.targetFrameRate = fps;
        OnTargetFrameRateChanged?.Invoke(fps);
    }

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, 1f, 10f);
        OnMouseSensitivityChanged?.Invoke(MouseSensitivity);
    }

    public void SetPoppyVoiceType(int voiceType)
    {
        PoppyVoiceType = Mathf.Clamp(voiceType, 1, 2); // 1=리나, 2=티모
        OnPoppyVoiceTypeChanged?.Invoke(PoppyVoiceType);
        Debug.Log($"[SettingManager] 뽀삐 목소리 변경: {PoppyVoiceType}");
    }

    public int GetPoppyVoiceType()
    {
        return PoppyVoiceType;
    }
}