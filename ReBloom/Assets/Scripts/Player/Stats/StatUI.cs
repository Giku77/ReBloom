using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatUI : MonoBehaviour
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
    }
}