using System;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager I { get; private set; }

    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 0.5f;
    public float SFXVolume { get; private set; } = 1f;

    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBGMVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
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
}