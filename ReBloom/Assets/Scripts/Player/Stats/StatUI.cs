using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatUI : UIBase
{
    [Header("References")]
    [SerializeField] private DebuffManager debuffManager;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerController playerController;

    [Header("StatBars")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider pollutionBar;
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Slider thirstBar;
    [SerializeField] private Slider tempBar;
    [SerializeField] private Image pollutionGauge;
    [SerializeField] private Image thirstGauge;
    [SerializeField] private Image hungerGauge;
    [SerializeField] private TextMeshProUGUI tempText;
    [SerializeField] private Image bodyTemperatureImage;

    [Header("StatTexts")]
    [SerializeField] private TextMeshProUGUI pollutionText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI thirstText;

    [Header("플레이어 데미지")]
    [SerializeField] private Image damageImage;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashSpeed = 1.0f;
    [SerializeField] private float lowHealthAlphaMin = 0.1f;
    [SerializeField] private float lowHealthAlphaMax = 0.5f;

    [Header("Debuff Blink Settings")]
    [SerializeField] private Color blinkColor = Color.red;
    [SerializeField] private float blinkDuration = 0.15f;
    [SerializeField] private int blinkCount = 3;

    [Header("Glow Images")]
    [SerializeField] private Image hpGlow;
    [SerializeField] private Image pollutionGlow;
    [SerializeField] private Image hungerGlow;
    [SerializeField] private Image thirstGlow;
    [SerializeField] private Image tempGlow;


    // 각 슬라이더의 원래 Fill 색상 저장
    private Dictionary<Slider, Color> originalFillColors = new Dictionary<Slider, Color>();

    // 깜빡임 Tween 관리 (중복 방지)
    private Dictionary<Slider, Tween> blinkTweens = new Dictionary<Slider, Tween>();

    // 디버프 메시지 정의
    private static readonly Dictionary<int, string> debuffMessages = new Dictionary<int, string>
    {
        // 갈증
        { 220, "목이 마르기 시작합니다..." },
        { 221, "심한 갈증을 느낍니다!" },
        { 222, "탈수 증상입니다!" },
        
        // 허기
        { 230, "배가 고프기 시작합니다..." },
        { 231, "심한 배고픔을 느낍니다!" },
        { 232, "기아 상태입니다! 빨리 음식을 드세요!" },
        
        // 오염
        { 210, "중독 상태입니다!" },
        
        // 체온
        { 240, "체온이 낮아지고 있습니다..." },
        { 250, "중증 저체온 증상입니다!" },
        { 260, "고열이 심해지고 있습니다!" },
        { 270, "이런! 열사병입니다!" },

        //{ 280, "온화한 날씨입니다" },
        //{ 281, "기온이 낮아졌습니다 (추위 1단계)" },
        { 282, "기온이 매우 낮아졌습니다 (추위 2단계)" },
        { 283, "매우 추운 날씨입니다 (추위 3단계)" },
        { 284, "날씨가 동사 직전입니다 (추위 4단계)" },

        //{ 285, "주변이 약간 덥습니다 (더위 1단계)" },
        { 286, "주변이 매우 덥습니다 (더위 2단계)" },
        { 287, "매우 뜨겁습니다 (더위 3단계)" },
    };


    private CancellationTokenSource lowHealthCTS;

    private void Start()
    {
        playerStats.OnStatChanged += HandleStatChanged;

        if (debuffManager == null)
        {
            debuffManager = playerStats.GetComponent<DebuffManager>();
        }

        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied += HandleDebuffApplied;
            debuffManager.OnDebuffRemoved += HandleDebuffRemoved;
        }

        InitializeUI();
        CacheOriginalColors();
        CacheGlowImages();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStatChanged -= HandleStatChanged;
        }

        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied -= HandleDebuffApplied;
            debuffManager.OnDebuffRemoved -= HandleDebuffRemoved;
        }

        lowHealthCTS?.Cancel();
        lowHealthCTS?.Dispose();

        foreach (var tween in blinkTweens.Values)
        {
            tween?.Kill();
        }
        blinkTweens.Clear();
    }

    private void CacheOriginalColors()
    {
        CacheSliderColor(pollutionBar);
        CacheSliderColor(hungerBar);
        CacheSliderColor(thirstBar);
        CacheSliderColor(tempBar);
        CacheSliderColor(hpBar);
    }

    private void CacheSliderColor(Slider slider)
    {
        if (slider == null) return;

        var fillImage = slider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            originalFillColors[slider] = fillImage.color;
        }
    }
    private Dictionary<Slider, Image> sliderToGlow = new Dictionary<Slider, Image>();

    private void CacheGlowImages()
    {
        // Slider와 Glow 이미지 매핑
        sliderToGlow[hpBar] = hpGlow;
        sliderToGlow[pollutionBar] = pollutionGlow;
        sliderToGlow[hungerBar] = hungerGlow;
        sliderToGlow[thirstBar] = thirstGlow;
        sliderToGlow[tempBar] = tempGlow;

        // 기본 비활성화
        foreach (var glow in sliderToGlow.Values)
        {
            if (glow != null)
                glow.gameObject.SetActive(false);
        }
    }

    private void InitializeUI()
    {
        UpdateHealthUI(playerStats.Health.Value, playerStats.Health.MaxValue);
        UpdatePollutionUI(playerStats.Pollution.Value, playerStats.Pollution.MaxValue);
        UpdateHungerUI(playerStats.Hunger.Value, playerStats.Hunger.MaxValue);
        UpdateThirstUI(playerStats.Thirst.Value, playerStats.Thirst.MaxValue);
        UpdateTemperatureUI(playerStats.Temperature.Value, playerStats.Temperature.MaxValue);

        UpdateHungerBarColor();
        UpdateThirstBarColor();
        UpdateTemperatureBarColor();

        if (damageImage != null)
        {
            damageImage.canvasRenderer.SetAlpha(0f);
        }
    }

    private void HandleStatChanged(StatBase stat, float oldValue, float newValue)
    {
        if (stat == playerStats.Health)
        {
            UpdateHealthUI(newValue, stat.MaxValue);

            if (oldValue - newValue >= 50)
                PlayHitEffect().Forget();

            if (newValue / stat.MaxValue <= 0.2f)
                StartLowHealthPulse().Forget();
            else
                lowHealthCTS?.Cancel();

        }
        else if (stat == playerStats.Pollution)
        {
            UpdatePollutionUI(newValue, stat.MaxValue);
        }
        else if (stat == playerStats.Hunger)
        {
            UpdateHungerUI(newValue, stat.MaxValue);
        }
        else if (stat == playerStats.Thirst)
        {
            UpdateThirstUI(newValue, stat.MaxValue);
        }
        else if (stat == playerStats.Temperature)
        {
            UpdateTemperatureUI(newValue, stat.MaxValue);
        }
    }

    private void HandleDebuffApplied(IDebuff debuff)
    {
        UpdateBarColorByDebuff(debuff.Category);

        // 깜빡임 효과 + Toast 메시지
        PlayDebuffWarning(debuff.ID);
    }

    private void HandleDebuffRemoved(IDebuff debuff)
    {
        UpdateBarColorByDebuff(debuff.Category);
    }

    private void UpdateBarColorByDebuff(int debuffCat)
    {
        if (debuffCat == 2)
        {
            UpdateThirstBarColor();
        }
        else if (debuffCat == 3)
        {
            UpdateHungerBarColor();
        }
        else if (debuffCat == 4 || debuffCat == 5 || debuffCat == 6 || debuffCat == 7)
        {
            UpdateTemperatureBarColor();
        }
    }

    private void UpdateThirstBarColor()
    {
        if (thirstBar == null || debuffManager == null) return;

        var fillImage = thirstBar.fillRect?.GetComponent<Image>();
        if (fillImage == null) return;

        if (debuffManager.HasDebuff(222))
        {
            fillImage.color = Color.red;
        }
        else if (debuffManager.HasDebuff(221))
        {
            fillImage.color = new Color(1f, 0.5f, 0f);
        }
        else if (debuffManager.HasDebuff(220))
        {
            fillImage.color = Color.yellow;
        }
        else
        {
            fillImage.color = Color.green;
        }
    }

    private void UpdateHungerBarColor()
    {
        if (hungerBar == null || debuffManager == null) return;

        var fillImage = hungerBar.fillRect?.GetComponent<Image>();
        if (fillImage == null) return;

        if (debuffManager.HasDebuff(232))
        {
            fillImage.color = Color.red;
        }
        else if (debuffManager.HasDebuff(231))
        {
            fillImage.color = new Color(1f, 0.5f, 0f);
        }
        else if (debuffManager.HasDebuff(230))
        {
            fillImage.color = Color.yellow;
        }
        else
        {
            fillImage.color = Color.green;
        }
    }

    private void UpdateTemperatureBarColor()
    {
        if (tempBar == null || debuffManager == null) return;

        var fillImager = tempBar.fillRect?.GetComponent<Image>();
        if (fillImager == null) return;

        if (debuffManager.HasDebuff(270) || debuffManager.HasDebuff(250))
        {
            fillImager.color = Color.red;
        }
        else if (debuffManager.HasDebuff(260) || debuffManager.HasDebuff(240))
        {
            fillImager.color = new Color(1f, 0.5f, 0f);
        }
        else
        {
            fillImager.color = Color.green;
        }
    }


    private void UpdateHealthUI(float value, float maxValue)
    {
        if (hpBar != null)
        {
            hpBar.value = value / maxValue;
        }

        if (hpText != null)
        {
            hpText.text = $"{((value / maxValue) * 100):F0}%";
        }
    }

    private void UpdatePollutionUI(float value, float maxValue)
    {
        if (pollutionBar != null)
        {
            pollutionBar.value = value / maxValue;
        }

        if (pollutionGauge != null)
        {
            pollutionGauge.fillAmount = value / 100;
        }

        if (pollutionText != null)
        {
            pollutionText.text = $"{((value / maxValue) * 100):F0}%";
        }
    }

    private void UpdateHungerUI(float value, float maxValue)
    {
        if (hungerBar != null)
        {
            hungerBar.value = value / maxValue;
        }

        if (hungerGauge != null)
        {
            hungerGauge.fillAmount = value / 100;
        }
        if (hungerText != null)
        {
            hungerText.text = $"{((value / maxValue) * 100):F0}%";
        }
    }

    private void UpdateThirstUI(float value, float maxValue)
    {
        if (thirstBar != null)
        {
            thirstBar.value = value / maxValue;
        }

        if (thirstGauge != null)
        {
            thirstGauge.fillAmount = value / 100;
        }
        if (thirstText != null)
        {
            thirstText.text = $"{((value / maxValue) * 100):F0}%";
        }
    }

    private void UpdateTemperatureUI(float value, float maxValue)
    {
        if (tempBar != null)
        {
            float minTemp = 31f;
            float maxTemp = playerStats.Temperature.MaxValue;

            //float realTemp = Mathf.Lerp(minTemp, maxTemp, value / maxValue);

            tempBar.minValue = minTemp;
            tempBar.maxValue = maxTemp;
            tempBar.value = value;
        }

        if (tempText != null)
        {
            tempText.text = $"{value:F1}";
        }

        if (bodyTemperatureImage != null)
        {
            bodyTemperatureImage.color = GetTempColor(value);
        }
    }

    /// <summary>
    /// 디버프 진입 시 슬라이더 깜빡임 + Toast 메시지
    /// </summary>
    private void PlayDebuffWarning(int debuffID)
    {
        // Toast 메시지
        if (debuffMessages.TryGetValue(debuffID, out string message))
        {
            ToastMessageUI.Instance?.Show(message, 2.5f);
        }

        // 해당 슬라이더 깜빡임
        Slider targetSlider = GetSliderForDebuff(debuffID);
        if (targetSlider != null)
        {
            PlaySliderBlink(targetSlider);
        }
    }

    /// <summary>
    /// 디버프 ID에 해당하는 슬라이더 반환
    /// </summary>
    private Slider GetSliderForDebuff(int debuffID)
    {
        return debuffID switch
        {
            210 => pollutionBar,                      // 오염
            >= 220 and <= 222 => thirstBar,           // 갈증
            >= 230 and <= 232 => hungerBar,           // 허기
            >= 240 and <= 270 => tempBar,             // 체온
            >= 280 and <= 287 => tempBar,             // 필드 온도
            _ => null
        };
    }

    /// <summary>
    /// 슬라이더 Glow 이미지 깜빡임 효과 (DOTween)
    /// </summary>
    private void PlaySliderBlink(Slider slider)
    {
        if (slider == null) return;

        // 해당 슬라이더의 Glow 이미지 찾기
        if (!sliderToGlow.TryGetValue(slider, out Image glowImage) || glowImage == null)
        {
            Debug.LogWarning($"[StatUI] {slider.name}의 Glow 이미지 없음");
            return;
        }

        // 기존 깜빡임 중이면 종료
        if (blinkTweens.TryGetValue(slider, out Tween existingTween))
        {
            existingTween?.Kill();
        }

        // Glow 활성화 + 알파 초기화
        glowImage.gameObject.SetActive(true);
        Color c = glowImage.color;
        c.a = 0f;
        glowImage.color = c;

        // 깜빡임 시퀀스
        Sequence blinkSequence = DOTween.Sequence();

        for (int i = 0; i < blinkCount; i++)
        {
            blinkSequence.Append(glowImage.DOFade(1f, blinkDuration).SetEase(Ease.InOutSine));
            blinkSequence.Append(glowImage.DOFade(0f, blinkDuration).SetEase(Ease.InOutSine));
        }

        blinkSequence.OnComplete(() =>
        {
            glowImage.gameObject.SetActive(false);
            blinkTweens.Remove(slider);
        });

        blinkTweens[slider] = blinkSequence;
    }

    public async UniTask PlayHitEffect()
    {
        if (damageImage != null)
        {
            damageImage.canvasRenderer.SetAlpha(lowHealthAlphaMax);
        }

        await UniTask.Delay((int)(flashDuration * 1000));

        if (damageImage != null)
        {
            damageImage.canvasRenderer.SetAlpha(0f);
        }
    }

    private async UniTask StartLowHealthPulse()
    {
        lowHealthCTS?.Cancel();
        lowHealthCTS = new CancellationTokenSource();
        CancellationToken token = lowHealthCTS.Token;

        //SoundManager.I?.StartBreathingHeavy();

        try
        {
            while (playerStats.Health.Value / playerStats.Health.MaxValue <= 0.2f)
            {
                float alpha = Mathf.Lerp(lowHealthAlphaMin, lowHealthAlphaMax,
                    (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f);

                if (damageImage != null)
                {
                    damageImage.canvasRenderer.SetAlpha(alpha);
                }
                await UniTask.Yield(token);
            }
            if (damageImage != null)
            {
                damageImage.canvasRenderer.SetAlpha(0f);
            }
        }
        catch (OperationCanceledException)
        {
            if (damageImage != null)
            {
                damageImage.canvasRenderer.SetAlpha(0f);
            }
        }

        //SoundManager.I?.StopBreathingHeavy();
    }

    private Color GetTempColor(float temp)
    {
        if (temp >= 40f)
            return Color.red;

        if (temp >= 38f)
            return new Color(1f, 0.55f, 0f); // 주황

        // 저체온
        if (temp <= 33f)
            return new Color(0.2f, 0.4f, 1f); // 파랑

        if (temp <= 35f)
            return new Color(0.5f, 0.75f, 1f); // 연파랑

        // 정상
        return Color.green;
    }
}