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
    [SerializeField] private Slider pollutionBar;
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Slider thirstBar;
    [SerializeField] private Slider tempBar;
    
void Start()
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
    
void OnDestroy()
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
    
void InitializeUI()
    {
        UpdateHealthUI(playerStats.Health.Value, playerStats.Health.MaxValue);
        UpdatePollutionUI(playerStats.Pollution.Value, playerStats.Pollution.MaxValue);
        UpdateHungerUI(playerStats.Hunger.Value, playerStats.Hunger.MaxValue);
        UpdateThirstUI(playerStats.Thirst.Value, playerStats.Thirst.MaxValue);
        
        UpdateHungerBarColor();
        UpdateThirstBarColor();
    }

    void HandleStatChanged(StatBase stat, float oldValue, float newValue)
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
    }

    void HandleDebuffApplied(IDebuff debuff)
    {
        UpdateBarColorByDebuff(debuff.Category);
    }
    
    void HandleDebuffRemoved(IDebuff debuff)
    {
        UpdateBarColorByDebuff(debuff.Category);
    }
    
    void UpdateBarColorByDebuff(int debuffCat)
    {
        if (debuffCat == 2)
        {
            UpdateThirstBarColor();
        }
        else if (debuffCat == 3)
        {
            UpdateHungerBarColor();
        }
    }
    
    void UpdateThirstBarColor()
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
    
    void UpdateHungerBarColor()
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

    
    void UpdateHealthUI(float value, float maxValue)
    {
        if (hpBar != null)
        {
            hpBar.value = value / maxValue;
        }
    }
    
    void UpdatePollutionUI(float value, float maxValue)
    {
        if (pollutionBar != null)
        {
            pollutionBar.value = value / maxValue;
        }
    }
    
void UpdateHungerUI(float value, float maxValue)
    {
        if (hungerBar != null)
        {
            hungerBar.value = value / maxValue;
        }
    }
    
void UpdateThirstUI(float value, float maxValue)
    {
        if (thirstBar != null)
        {
            thirstBar.value = value / maxValue;
        }
    }
}