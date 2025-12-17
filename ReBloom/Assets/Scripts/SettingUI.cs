using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : UIBase
{
    [Header("사운드 설정")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("그래픽 설정")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;

    private Resolution[] resolutions;

    protected override void Awake()
    {
        base.Awake();

        InitResolutionDropdown();
    }

    protected override void OnShow()
    {
        Time.timeScale = 0f;
        SoundManager.I?.PlayOpenInventory();
        UIManager.Instance?.SetPaused(true);

        masterSlider.SetValueWithoutNotify(SettingManager.I.MasterVolume);
        bgmSlider.SetValueWithoutNotify(SettingManager.I.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SettingManager.I.SFXVolume);

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        fullscreenToggle.SetIsOnWithoutNotify(SettingManager.I.IsFullScreen);
        vsyncToggle.SetIsOnWithoutNotify(SettingManager.I.IsVSyncEnabled);

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
        vsyncToggle.onValueChanged.AddListener(OnVSyncToggle);
    }

    protected override void OnHide()
    {
        SoundManager.I?.PlayCloseInventory();
        UIManager.Instance?.ShowUI(UIType.GamePause);

        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        vsyncToggle.onValueChanged.RemoveAllListeners();
    }

    private void OnMasterChanged(float value)
    {
        SettingManager.I.SetMasterVolume(value);
    }

    private void OnBGMChanged(float value)
    {
        SettingManager.I.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        SettingManager.I.SetSFXVolume(value);
    }

    private void InitResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            string option = $"{r.width} x {r.height}";
            options.Add(option);

            if (r.width == SettingManager.I.CurrentResolution.width &&
                r.height == SettingManager.I.CurrentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void OnResolutionChanged(int index)
    {
        Resolution r = resolutions[index];
        SettingManager.I.SetResolution(r);
    }

    private void OnFullscreenToggle(bool isFullscreen)
    {
        SettingManager.I.SetFullScreen(isFullscreen);
    }

    private void OnVSyncToggle(bool enabled)
    {
        SettingManager.I.SetVSync(enabled);
    }
}
