using UnityEngine;
using UnityEngine.UI;

public class SettingUI : UIBase
{
    [Header("사운드 설정")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

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
    }

    protected override void OnHide()
    {
        SoundManager.I?.PlayCloseInventory();
        UIManager.Instance?.ShowUI(UIType.GamePause);

        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
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
}
