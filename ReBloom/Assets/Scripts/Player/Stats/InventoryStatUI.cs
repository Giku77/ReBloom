using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class InventoryStatUI : MonoBehaviour
{
    [Header("References (bound from local player)")]
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

    [Header("Player Damage")]
    [SerializeField] private Image damageImage;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashSpeed = 1.0f;
    [SerializeField] private float lowHealthAlphaMin = 0.1f;
    [SerializeField] private float lowHealthAlphaMax = 0.5f;

    [Header("Platform Layout")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Vector2 pcAnchorMin = new Vector2(0, 0);
    [SerializeField] private Vector2 pcAnchorMax = new Vector2(0, 0);
    [SerializeField] private Vector2 pcAnchoredPosition;
    [SerializeField] private Vector2 mobileAnchorMin = new Vector2(0.5f, 0);
    [SerializeField] private Vector2 mobileAnchorMax = new Vector2(0.5f, 0);
    [SerializeField] private Vector2 mobilePivot = new Vector2(0.5f, 0);
    [SerializeField] private Vector2 mobileAnchoredPosition;

    [SerializeField] private GameObject protectiveUI;

    private CancellationTokenSource lowHealthCTS;
    private bool isBound;

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;

        ClearUI();
        TryBindFromExistingLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void TryBindFromExistingLocalPlayer()
    {
        GameObject playerObject = ResolveExistingLocalPlayerObject();
        if (playerObject != null)
            BindLocalPlayer(playerObject);
    }

    private GameObject ResolveExistingLocalPlayerObject()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            var localPlayerObject = nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
            if (localPlayerObject != null)
                return localPlayerObject.gameObject;

            if (nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
                return nm.LocalClient.PlayerObject.gameObject;
        }

        var ownerGates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
        foreach (var gate in ownerGates)
        {
            if (gate != null && gate.IsOwner)
                return gate.gameObject;
        }

        return null;
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null)
            return;

        if (isBound && playerController != null && playerController.gameObject == playerObj)
            return;

        var nextController = playerObj.GetComponent<PlayerController>();
        var nextStats = playerObj.GetComponent<PlayerStats>();
        var nextDebuffManager = playerObj.GetComponent<DebuffManager>();

        if (nextController == null || nextStats == null)
            return;

        Unbind();

        playerController = nextController;
        playerStats = nextStats;
        debuffManager = nextDebuffManager;

        playerStats.OnStatChanged += HandleStatChanged;

        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied += HandleDebuffApplied;
            debuffManager.OnDebuffRemoved += HandleDebuffRemoved;
        }

        isBound = true;
        InitializeUI();
        Debug.Log($"[InventoryStatUI] Local player bound. hp={playerStats.Health.Value:F1}/{playerStats.Health.MaxValue:F1}");
    }

    private void HandleLocalPlayerDespawned()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (playerStats != null)
            playerStats.OnStatChanged -= HandleStatChanged;

        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied -= HandleDebuffApplied;
            debuffManager.OnDebuffRemoved -= HandleDebuffRemoved;
        }

        lowHealthCTS?.Cancel();
        lowHealthCTS?.Dispose();
        lowHealthCTS = null;

        isBound = false;
        playerController = null;
        playerStats = null;
        debuffManager = null;

        ClearUI();
    }

    private void InitializeUI()
    {
        if (playerStats == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        SetPositionByPlatform();

        UpdateHealthUI(playerStats.Health.Value, playerStats.Health.MaxValue);
        UpdatePollutionUI(playerStats.Pollution.Value, playerStats.Pollution.MaxValue);
        UpdateHungerUI(playerStats.Hunger.Value, playerStats.Hunger.MaxValue);
        UpdateThirstUI(playerStats.Thirst.Value, playerStats.Thirst.MaxValue);
        UpdateTemperatureUI(playerStats.Temperature.Value);

        UpdateHungerBarColor();
        UpdateThirstBarColor();
        UpdateTemperatureBarColor();

        if (damageImage != null)
            damageImage.canvasRenderer.SetAlpha(0f);
    }

    private void SetPositionByPlatform()
    {
        if (rectTransform == null) return;

        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            rectTransform.anchorMin = mobileAnchorMin;
            rectTransform.anchorMax = mobileAnchorMax;
            rectTransform.pivot = mobilePivot;
            rectTransform.anchoredPosition = mobileAnchoredPosition;

            if (protectiveUI != null)
                protectiveUI.SetActive(false);
        }
        else
        {
            rectTransform.anchorMin = pcAnchorMin;
            rectTransform.anchorMax = pcAnchorMax;
            rectTransform.anchoredPosition = pcAnchoredPosition;
        }
    }

    private void ClearUI()
    {
        SetSliderValue(hpBar, 0f, 0f, 100f);
        SetSliderValue(pollutionBar, 0f, 0f, 100f);
        SetSliderValue(hungerBar, 0f, 0f, 100f);
        SetSliderValue(thirstBar, 0f, 0f, 100f);
        SetSliderValue(tempBar, 0f, 0f, 1f);

        SetGaugeFill(pollutionGauge, 0f, 100f);
        SetGaugeFill(hungerGauge, 0f, 100f);
        SetGaugeFill(thirstGauge, 0f, 100f);

        if (hpText != null) hpText.text = "0%";
        if (pollutionText != null) pollutionText.text = "0%";
        if (hungerText != null) hungerText.text = "0%";
        if (thirstText != null) thirstText.text = "0%";
        if (tempText != null) tempText.text = "-";
        if (bodyTemperatureImage != null) bodyTemperatureImage.color = Color.green;
        if (damageImage != null) damageImage.canvasRenderer.SetAlpha(0f);
    }

    private void HandleStatChanged(StatBase stat, float oldValue, float newValue)
    {
        if (playerStats == null)
            return;

        if (stat == playerStats.Health)
        {
            UpdateHealthUI(newValue, stat.MaxValue);

            if (oldValue - newValue >= 50f)
                PlayHitEffect().Forget();

            if (newValue / Mathf.Max(stat.MaxValue, 0.0001f) <= 0.2f)
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
            UpdateTemperatureUI(newValue);
        }
    }

    private void HandleDebuffApplied(IDebuff debuff) => UpdateBarColorByDebuff(debuff.Category);
    private void HandleDebuffRemoved(IDebuff debuff) => UpdateBarColorByDebuff(debuff.Category);

    private void UpdateBarColorByDebuff(int debuffCat)
    {
        if (debuffCat == 2) UpdateThirstBarColor();
        else if (debuffCat == 3) UpdateHungerBarColor();
        else if (debuffCat == 4 || debuffCat == 5 || debuffCat == 6 || debuffCat == 7) UpdateTemperatureBarColor();
    }

    private void UpdateThirstBarColor()
    {
        if (thirstBar == null || debuffManager == null) return;
    }

    private void UpdateHungerBarColor()
    {
        if (hungerBar == null || debuffManager == null) return;
    }

    private void UpdateTemperatureBarColor()
    {
        if (tempBar == null || debuffManager == null) return;

        var fillImager = tempBar.fillRect?.GetComponent<Image>();
        if (fillImager == null) return;

        if (debuffManager.HasDebuff(270) || debuffManager.HasDebuff(250))
            fillImager.color = Color.red;
        else if (debuffManager.HasDebuff(260) || debuffManager.HasDebuff(240))
            fillImager.color = new Color(1f, 0.5f, 0f);
        else
            fillImager.color = Color.green;
    }

    private void UpdateHealthUI(float value, float maxValue)
    {
        SetSliderValue(hpBar, value, 0f, maxValue);
        if (hpText != null) hpText.text = $"{GetPercent(value, maxValue):F0}%";
    }

    private void UpdatePollutionUI(float value, float maxValue)
    {
        SetSliderValue(pollutionBar, value, 0f, maxValue);
        SetGaugeFill(pollutionGauge, value, maxValue);
        if (pollutionText != null) pollutionText.text = $"{GetPercent(value, maxValue):F0}%";
    }

    private void UpdateHungerUI(float value, float maxValue)
    {
        SetSliderValue(hungerBar, value, 0f, maxValue);
        SetGaugeFill(hungerGauge, value, maxValue);
        if (hungerText != null) hungerText.text = $"{GetPercent(value, maxValue):F0}%";
    }

    private void UpdateThirstUI(float value, float maxValue)
    {
        SetSliderValue(thirstBar, value, 0f, maxValue);
        SetGaugeFill(thirstGauge, value, maxValue);
        if (thirstText != null) thirstText.text = $"{GetPercent(value, maxValue):F0}%";
    }

    private void UpdateTemperatureUI(float value)
    {
        float minTemp = GetMinTemperature();
        float maxTemp = GetMaxTemperature();

        SetSliderValue(tempBar, value, minTemp, maxTemp);

        if (tempText != null) tempText.text = $"{value:F1}";
        if (bodyTemperatureImage != null) bodyTemperatureImage.color = GetTempColor(value);
    }

    private float GetMinTemperature()
    {
        if (playerStats != null && playerStats.data != null)
            return playerStats.data.minTemperature;

        return 31f;
    }

    private float GetMaxTemperature()
    {
        if (playerStats != null && playerStats.Temperature != null)
            return playerStats.Temperature.MaxValue;

        return 43f;
    }

    private static void SetSliderValue(Slider slider, float value, float minValue, float maxValue)
    {
        if (slider == null)
            return;

        float safeMax = Mathf.Max(maxValue, minValue + 0.0001f);
        slider.minValue = minValue;
        slider.maxValue = safeMax;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, minValue, safeMax));
    }

    private static void SetGaugeFill(Image image, float value, float maxValue)
    {
        if (image == null)
            return;

        image.fillAmount = Mathf.Clamp01(value / Mathf.Max(maxValue, 0.0001f));
    }

    private static float GetPercent(float value, float maxValue)
    {
        return Mathf.Clamp01(value / Mathf.Max(maxValue, 0.0001f)) * 100f;
    }

    public async UniTask PlayHitEffect()
    {
        if (damageImage != null) damageImage.canvasRenderer.SetAlpha(lowHealthAlphaMax);
        await UniTask.Delay((int)(flashDuration * 1000));
        if (damageImage != null) damageImage.canvasRenderer.SetAlpha(0f);
    }

    private async UniTask StartLowHealthPulse()
    {
        if (playerStats == null) return;

        lowHealthCTS?.Cancel();
        lowHealthCTS = new CancellationTokenSource();
        var token = lowHealthCTS.Token;

        try
        {
            while (playerStats.Health.Value / Mathf.Max(playerStats.Health.MaxValue, 0.0001f) <= 0.2f)
            {
                float alpha = Mathf.Lerp(lowHealthAlphaMin, lowHealthAlphaMax,
                    (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f);

                if (damageImage != null)
                    damageImage.canvasRenderer.SetAlpha(alpha);

                await UniTask.Yield(token);
            }

            if (damageImage != null)
                damageImage.canvasRenderer.SetAlpha(0f);
        }
        catch (OperationCanceledException)
        {
            if (damageImage != null)
                damageImage.canvasRenderer.SetAlpha(0f);
        }
    }

    private Color GetTempColor(float temp)
    {
        if (temp >= 40f) return Color.red;
        if (temp >= 38f) return new Color(1f, 0.55f, 0f);
        if (temp <= 33f) return new Color(0.2f, 0.4f, 1f);
        if (temp <= 35f) return new Color(0.5f, 0.75f, 1f);
        return Color.green;
    }
}
