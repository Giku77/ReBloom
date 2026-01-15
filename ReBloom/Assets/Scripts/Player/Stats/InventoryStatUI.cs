using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("플레이어 데미지")]
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

        TryBindFromExistingLocalPlayer();
    }
    private void TryBindFromExistingLocalPlayer()
    {
        var all = FindObjectsByType<Unity.Netcode.NetworkObject>(FindObjectsSortMode.None);
        foreach (var no in all)
        {
            if (no != null && no.IsOwner)
            {
                BindLocalPlayer(no.gameObject);
                return;
            }
        }
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        // 같은 오브젝트에 이미 바인딩되어 있으면 중복 방지
        if (isBound && playerController != null && playerController.gameObject == playerObj)
            return;

        Unbind();

        playerController = playerObj.GetComponent<PlayerController>();
        playerStats = playerObj.GetComponent<PlayerStats>();
        debuffManager = playerObj.GetComponent<DebuffManager>();

        if (playerStats == null)
        {
            //Debug.LogError("[InventoryStatUI] PlayerStats 없음");
            return;
        }

        // 이벤트 구독은 바인딩 후
        playerStats.OnStatChanged += HandleStatChanged;

        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied += HandleDebuffApplied;
            debuffManager.OnDebuffRemoved += HandleDebuffRemoved;
        }

        isBound = true;

        InitializeUI();
        Debug.Log("[InventoryStatUI] Local player bound.");
    }

    private void Unbind()
    {
        // 이벤트 해제
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

        // 참조 정리(원하면 유지해도 됨)
        playerController = null;
        playerStats = null;
        debuffManager = null;
    }

    private void InitializeUI()
    {
        if (playerStats == null) return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        SetPositionByPlatform();

        UpdateHealthUI(playerStats.Health.Value, playerStats.Health.MaxValue);
        UpdatePollutionUI(playerStats.Pollution.Value, playerStats.Pollution.MaxValue);
        UpdateHungerUI(playerStats.Hunger.Value, playerStats.Hunger.MaxValue);
        UpdateThirstUI(playerStats.Thirst.Value, playerStats.Thirst.MaxValue);
        UpdateTemperatureUI(playerStats.Temperature.Value, playerStats.Temperature.MaxValue);

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

    private void HandleStatChanged(StatBase stat, float oldValue, float newValue)
    {
        if (playerStats == null) return;

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
        // 기존 로직 유지(주석 해제 가능)
    }

    private void UpdateHungerBarColor()
    {
        if (hungerBar == null || debuffManager == null) return;
        // 기존 로직 유지(주석 해제 가능)
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
        if (hpBar != null) hpBar.value = value / maxValue;
        if (hpText != null) hpText.text = $"{((value / maxValue) * 100):F0}%";
    }

    private void UpdatePollutionUI(float value, float maxValue)
    {
        if (pollutionBar != null) pollutionBar.value = value / maxValue;
        if (pollutionGauge != null) pollutionGauge.fillAmount = value / 100;
        if (pollutionText != null) pollutionText.text = $"{((value / maxValue) * 100):F0}%";
    }

    private void UpdateHungerUI(float value, float maxValue)
    {
        if (hungerBar != null) hungerBar.value = value / maxValue;
        if (hungerGauge != null) hungerGauge.fillAmount = value / 100;
        if (hungerText != null) hungerText.text = $"{((value / maxValue) * 100):F0}%";
    }

    private void UpdateThirstUI(float value, float maxValue)
    {
        if (thirstBar != null) thirstBar.value = value / maxValue;
        if (thirstGauge != null) thirstGauge.fillAmount = value / 100;
        if (thirstText != null) thirstText.text = $"{((value / maxValue) * 100):F0}%";
    }

    private void UpdateTemperatureUI(float value, float maxValue)
    {
        if (tempBar != null)
        {
            float minTemp = 31f;
            float maxTemp = playerStats.Temperature.MaxValue;

            tempBar.minValue = minTemp;
            tempBar.maxValue = maxTemp;
            tempBar.value = value;
        }

        if (tempText != null) tempText.text = $"{value:F1}";
        if (bodyTemperatureImage != null) bodyTemperatureImage.color = GetTempColor(value);
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
            while (playerStats.Health.Value / playerStats.Health.MaxValue <= 0.2f)
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
