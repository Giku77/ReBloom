using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingView : MonoBehaviour
{
    public event Action OnBackRequested;

    [Header("사운드 설정")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("그래픽 설정")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private TMP_Dropdown frameRateDropdown;

    [Header("조작 설정")]
    [SerializeField] private Slider mouseSensitivitySlider;

    [Header("Buttons")]
    [SerializeField] private Button soundSettingButton;
    [SerializeField] private Button graphicSettingButton;
    [SerializeField] private Button controlSettingButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField] private GameObject soundSettingPanel;
    [SerializeField] private GameObject graphicSettingPanel;
    [SerializeField] private GameObject controlSettingPanel;

    private Resolution[] resolutions;
    private bool isInitialized = false;

    private void Awake()
    {
        soundSettingButton.onClick.AddListener(() => ShowPanel(soundSettingPanel));
        graphicSettingButton.onClick.AddListener(() => ShowPanel(graphicSettingPanel));
        controlSettingButton.onClick.AddListener(() => ShowPanel(controlSettingPanel));
        backButton.onClick.AddListener(() => OnBackRequested?.Invoke());
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (!isInitialized)
        {
            Init();
            isInitialized = true;
        }

        AddListeners();
    }

    public void Hide()
    {
        RemoveListeners();
        gameObject.SetActive(false);
    }

    private void Init()
    {
        InitResolutionDropdown();
        InitGraphicsQualityDropdown();
        InitFrameRateDropdown();

        ShowPanel(soundSettingPanel);

        masterSlider.SetValueWithoutNotify(SettingManager.I.MasterVolume);
        bgmSlider.SetValueWithoutNotify(SettingManager.I.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SettingManager.I.SFXVolume);
        mouseSensitivitySlider.SetValueWithoutNotify(SettingManager.I.MouseSensitivity);

        fullscreenToggle.SetIsOnWithoutNotify(SettingManager.I.IsFullScreen);
        vsyncToggle.SetIsOnWithoutNotify(SettingManager.I.IsVSyncEnabled);
        graphicsQualityDropdown.SetValueWithoutNotify(SettingManager.I.GraphicsQuality);
        frameRateDropdown.SetValueWithoutNotify(GetFrameRateDropdownIndex(SettingManager.I.TargetFrameRate));
    }

    private void AddListeners()
    {
        if (SettingManager.I == null) return;

        masterSlider.onValueChanged.AddListener(SettingManager.I.SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SettingManager.I.SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SettingManager.I.SetSFXVolume);

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(SettingManager.I.SetFullScreen);
        vsyncToggle.onValueChanged.AddListener(SettingManager.I.SetVSync);
        graphicsQualityDropdown.onValueChanged.AddListener(SettingManager.I.SetGraphicsQuality);
        frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);

        mouseSensitivitySlider.onValueChanged.AddListener(SettingManager.I.SetMouseSensitivity);
    }

    private void RemoveListeners()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        vsyncToggle.onValueChanged.RemoveAllListeners();
        graphicsQualityDropdown.onValueChanged.RemoveAllListeners();
        frameRateDropdown.onValueChanged.RemoveAllListeners();

        mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
    }

    private void InitResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError("Resolution Dropdown not assigned");
            return;
        }

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            options.Add($"{r.width} x {r.height}");

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

    private void InitGraphicsQualityDropdown()
    {
        graphicsQualityDropdown.ClearOptions();
        graphicsQualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    private void InitFrameRateDropdown()
    {
        frameRateDropdown.ClearOptions();
        frameRateDropdown.AddOptions(new List<string>
        {
            "30 FPS",
            "60 FPS",
            "120 FPS",
            "144 FPS",
            "무제한"
        });
    }

    private void OnResolutionChanged(int index)
    {
        if (SettingManager.I != null && resolutions != null && index < resolutions.Length)
        {
            SettingManager.I.SetResolution(resolutions[index]);
        }
    }

    private void OnFrameRateChanged(int index)
    {
        if (SettingManager.I == null) return;

        int fps = index switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            3 => 144,
            _ => -1
        };

        SettingManager.I.SetTargetFrameRate(fps);
    }

    private int GetFrameRateDropdownIndex(int fps)
    {
        return fps switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            144 => 3,
            _ => 4
        };
    }

    private void ShowPanel(GameObject target)
    {
        soundSettingPanel.SetActive(false);
        graphicSettingPanel.SetActive(false);
        controlSettingPanel.SetActive(false);

        target.SetActive(true);
        SoundManager.I?.PlayUIClick();
    }
}