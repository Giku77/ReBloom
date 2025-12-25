using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class MobileMainUI : UIBase
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractable playerInteractable;
    [SerializeField] private ScanController scanController;
    [SerializeField] private StageDetector stageDetector;
    [SerializeField] private PlayerStats playerStats;

    [Header("Joystick")]
    [SerializeField] private FixedJoystick movementJoystick;

    [Header("Buttons")]
    [SerializeField] private Button sprintToggleButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button lightButton;
    [SerializeField] private Button settingButton;

    [Header("UI Feedback")]
    [SerializeField] private GameObject runImage;
    [SerializeField] private GameObject walkImage;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private TextMeshProUGUI currentDayText;
    [SerializeField] private TextMeshProUGUI currentTimeText;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    private float updateTimer = 0f;

    private bool isSprinting = false;

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (stageDetector == null && playerStats != null)
        {
            stageDetector = playerStats.GetComponent<StageDetector>();
        }

        UpdateWeatherUI();
    }

    private void UpdateWeatherUI()
    {
        if (playerStats == null) return;

        if (weatherText != null)
        {
            string weather = GetCurrentWeather();
            weatherText.text = weather;
        }

        if (stageText != null)
        {
            string location = GetCurrentLocation();
            stageText.text = location;
        }

        if (currentDayText != null)
        {
            string day = GetCurrentDay();
            currentDayText.text = day;
        }

        if (currentTimeText != null)
        {
            string time = GetCurrentTime();
            currentTimeText.text = time;
        }

        if (temperatureText != null)
        {
            string currentTemp = GetCurrentTemperature();
            temperatureText.text = currentTemp;
        }
    }

    private string GetCurrentWeather()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "Sunny";

        return stageDetector.CurrentStage.CurrentWeather.ToString();
    }

    private string GetCurrentLocation()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "알 수 없음";

        if (stageDetector.CurrentStage.Data != null)
        {
            return stageDetector.CurrentStage.Data.name;
        }

        return "알 수 없음";
    }
    private string GetCurrentDay()
    {
        if (DayNightCycle.Instance == null)
            return "1일차";

        return $"{DayNightCycle.Instance.CurrentDay}일차";
    }

    private string GetCurrentTime()
    {
        if (DayNightCycle.Instance == null)
            return "00시 00분";

        int hour = DayNightCycle.Instance.GetCurrentHour();
        int minute = DayNightCycle.Instance.GetCurrentMinute();

        return $"{hour:D2}시 {minute:D2}분";
    }

    private string GetCurrentTemperature()
    {
        if (stageDetector == null || stageDetector.CurrentStage == null)
            return "36.5°C";

        if (stageDetector.CurrentStage.Data != null)
        {
            return $"{stageDetector.GetCurrentTemperatureMultiplier():F1}°C";
        }

        return "36.5°C";
    }

    protected override void OnShow()
    {
        base.OnShow();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.AddListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpClicked);

        if (exploreButton != null)
            exploreButton.onClick.AddListener(OnExploreClicked);


        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryOpenClicked);


        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildClicked);

        if (settingButton != null)
            settingButton.onClick.AddListener(OnSettingClicked);

        if (lightButton != null)
            lightButton.onClick.AddListener(OnFlashLightClicked);
        //if (interactButton != null)
        //    interactButton.onClick.AddListener(OnInteract);



        UpdateRunImage();
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.RemoveListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpClicked);

        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(OnInventoryOpenClicked);

        //if (interactButton != null)
        //    interactButton.onClick.RemoveListener(OnInteract);
    }

    private void Update()
    {
        if (playerController == null || movementJoystick == null) return;

        Vector2 input = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);

        playerController.SetMobileInput(input, isSprinting);

        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateWeatherUI();
        }
    }

    private void OnSprintToggle()
    {
        isSprinting = !isSprinting;
        UpdateRunImage();
    }

    private void OnJumpClicked()
    {
        if (playerController != null)
        {
            playerController.RequestJump();
        }
    }

    private void UpdateRunImage()
    {
        if (runImage == null || walkImage == null) return;

        if (isSprinting)
        {
            runImage.SetActive(false);
            walkImage.SetActive(true);
        }
        else
        { 
            walkImage.SetActive(false);
            runImage.SetActive(true);
        }
    }

    public void OnInteractDown(BaseEventData data)
    {
        if (playerInteractable != null)
            playerInteractable.TriggerInteract();
    }

    public void OnInteractUp(BaseEventData data)
    {
        if (playerInteractable != null)
            playerInteractable.CancelMobileInteract();
    }

    private void OnInventoryOpenClicked()
    {
        UIManager.Instance?.ShowUI(UIType.Inventory);
    }

    private void OnBuildClicked()
    {
        UIManager.Instance?.ShowUI(UIType.Building);
    }

    private void OnExploreClicked()
    {
        scanController?.TriggerScan();
    }

    private void OnFlashLightClicked()
    {
        if (playerController.RobotPet == null) return;

        playerController.RobotPet.ToggleFlashlight();
    }

    private void OnSettingClicked()
    { 
        UIManager.Instance?.ShowUI(UIType.GamePause);
    }
}