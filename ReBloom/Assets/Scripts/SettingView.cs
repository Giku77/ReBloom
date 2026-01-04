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

    [Header("뽀삐 목소리 설정")]
    [SerializeField] private Button poppyVoiceLeftButton;
    [SerializeField] private Button poppyVoiceRightButton;
    [SerializeField] private TextMeshProUGUI poppyVoiceNameText;

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

    private readonly string[] poppyVoiceNames = { "리나", "티모" };
    private int currentPoppyVoiceIndex = 0;
    private readonly int[] poppySampleVoiceIds = { 80050, 80051, 80065, 80066, 80067 };

    private Resolution[] resolutions;
    private bool isInitialized = false;

    private void Awake()
    {
        soundSettingButton.onClick.AddListener(() => ShowPanel(soundSettingPanel));
        graphicSettingButton.onClick.AddListener(() => ShowPanel(graphicSettingPanel));
        controlSettingButton.onClick.AddListener(() => ShowPanel(controlSettingPanel));
        backButton.onClick.AddListener(() => OnBackRequested?.Invoke());

        poppyVoiceLeftButton.onClick.AddListener(OnPoppyVoiceLeftClicked);
        poppyVoiceRightButton.onClick.AddListener(OnPoppyVoiceRightClicked);
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
        bool isMobile = PlatformManager.Instance != null && PlatformManager.Instance.IsMobile;

        if (!isMobile)
        {
            InitResolutionDropdown();
        }

        InitGraphicsQualityDropdown();
        InitFrameRateDropdown();

        ShowPanel(soundSettingPanel);

        masterSlider.SetValueWithoutNotify(SettingManager.I.MasterVolume);
        bgmSlider.SetValueWithoutNotify(SettingManager.I.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SettingManager.I.SFXVolume);
        mouseSensitivitySlider.SetValueWithoutNotify(SettingManager.I.MouseSensitivity);

        if (isMobile)
        {
            HidePCOnlySettings();
        }
        else
        {
            fullscreenToggle.SetIsOnWithoutNotify(SettingManager.I.IsFullScreen);
            vsyncToggle.SetIsOnWithoutNotify(SettingManager.I.IsVSyncEnabled);
        }

        graphicsQualityDropdown.SetValueWithoutNotify(SettingManager.I.GraphicsQuality);
        frameRateDropdown.SetValueWithoutNotify(GetFrameRateDropdownIndex(SettingManager.I.TargetFrameRate));

        currentPoppyVoiceIndex = SettingManager.I.GetPoppyVoiceType() - 1;
        UpdatePoppyVoiceUI();
    }

    private void AddListeners()
    {
        if (SettingManager.I == null) return;

        masterSlider.onValueChanged.AddListener(SettingManager.I.SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SettingManager.I.SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SettingManager.I.SetSFXVolume);
        mouseSensitivitySlider.onValueChanged.AddListener(SettingManager.I.SetMouseSensitivity);

        bool isMobile = PlatformManager.Instance != null && PlatformManager.Instance.IsMobile;

        if (!isMobile)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle.onValueChanged.AddListener(SettingManager.I.SetFullScreen);
            vsyncToggle.onValueChanged.AddListener(SettingManager.I.SetVSync);
        }
        graphicsQualityDropdown.onValueChanged.AddListener(SettingManager.I.SetGraphicsQuality);
        frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);


    }

    private void RemoveListeners()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        mouseSensitivitySlider.onValueChanged.RemoveAllListeners();

        bool isMobile = PlatformManager.Instance != null && PlatformManager.Instance.IsMobile;

        if (!isMobile)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.onValueChanged.RemoveAllListeners();
        }
        graphicsQualityDropdown.onValueChanged.RemoveAllListeners();
        frameRateDropdown.onValueChanged.RemoveAllListeners();
    }

    private void OnPoppyVoiceLeftClicked()
    {
        currentPoppyVoiceIndex--;
        if (currentPoppyVoiceIndex < 0)
            currentPoppyVoiceIndex = poppyVoiceNames.Length - 1;

        UpdatePoppyVoice();
    }

    private void OnPoppyVoiceRightClicked()
    {
        currentPoppyVoiceIndex++;
        if (currentPoppyVoiceIndex >= poppyVoiceNames.Length)
            currentPoppyVoiceIndex = 0;

        UpdatePoppyVoice();
    }

    private void UpdatePoppyVoice()
    {
        int voiceType = currentPoppyVoiceIndex + 1; // 0-based -> 1-based

        // SettingManager에 저장
        SettingManager.I.SetPoppyVoiceType(voiceType);

        // VoiceManager에 적용
        if (VoiceManager.I != null)
        {
            VoiceManager.I.SetPoppyVoiceType(voiceType);
        }

        // UI 업데이트
        UpdatePoppyVoiceUI();

        // 샘플 음성 재생 (랜덤)
        PlayRandomPoppySample();
    }

    private void UpdatePoppyVoiceUI()
    {
        if (poppyVoiceNameText != null)
        {
            poppyVoiceNameText.text = poppyVoiceNames[currentPoppyVoiceIndex];
        }
    }

    private void PlayRandomPoppySample()
    {
        if (VoiceManager.I == null || poppySampleVoiceIds.Length == 0)
            return;

        int randomIndex = UnityEngine.Random.Range(0, poppySampleVoiceIds.Length);
        int sampleVoiceId = poppySampleVoiceIds[randomIndex];

        VoiceManager.I.PlayVoice(sampleVoiceId);
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
        //SoundManager.I?.PlayUIClick();
    }

    private void HidePCOnlySettings()
    {
        // 해상도 관련
        if (resolutionDropdown != null)
            resolutionDropdown.transform.parent.gameObject.SetActive(false);

        // 전체화면
        if (fullscreenToggle != null)
            fullscreenToggle.transform.parent.gameObject.SetActive(false);

        // VSync
        if (vsyncToggle != null)
            vsyncToggle.transform.parent.gameObject.SetActive(false);
    }
}