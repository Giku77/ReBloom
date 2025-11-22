using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatUI : MonoBehaviour
{
    [Header("References")]
    
    [SerializeField] private DebuffManager debuffManager;
    [SerializeField] private PlayerStats playerStats;
    
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
    }

    private void HandleStatChanged(StatBase stat, float oldValue, float newValue)
    {
        if (stat == playerStats.Health)
        {
            UpdateHealthUI(newValue, stat.MaxValue);
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
    }
}