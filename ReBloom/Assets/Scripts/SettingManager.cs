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

    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBGMVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<Resolution> OnResolutionChanged;
    public event Action<bool> OnFullScreenChanged;
    public event Action<bool> OnVSyncChanged;
    public event Action<float> OnMouseSensitivityChanged;

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

    public void SetFullScreen(bool isFullScreen)
    {
        IsFullScreen = isFullScreen;
        Screen.fullScreen = isFullScreen;
        Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, isFullScreen);
        OnFullScreenChanged?.Invoke(isFullScreen);
    }

    public void SetVSync(bool enabled)
    {
        IsVSyncEnabled = enabled;
        // VSync: 0 = 끔, 1 = 60fps 제한, 2 = 30fps 제한 
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        OnVSyncChanged?.Invoke(enabled);
    }

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, 1f, 10f);
        OnMouseSensitivityChanged?.Invoke(MouseSensitivity);
    }
}