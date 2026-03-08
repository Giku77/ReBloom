using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaterTankUI : UIBase
{
    [SerializeField] private Image backgroundImage;

    [Header("UI")]
    [SerializeField] private Button storeWaterButton;
    [SerializeField] private Button retrieveWaterButton;
    [SerializeField] private Button storeContaminatedWaterButton;
    [SerializeField] private TextMeshProUGUI waterLevelText;
    [SerializeField] private Slider waterTankSlider;

    private WaterTankInteractable currentTank;

    protected override void Awake()
    {
        base.Awake();

        storeWaterButton.onClick.AddListener(OnStoreWaterButtonClicked);
        retrieveWaterButton.onClick.AddListener(OnRetrieveWaterButtonClicked);
        storeContaminatedWaterButton.onClick.AddListener(OnstoreContaminatedWaterButtonClicked);

        if (waterTankSlider != null)
        {
            waterTankSlider.minValue = 0f;
            waterTankSlider.maxValue = 100f;
            waterTankSlider.value = 0f;
        }

        ChangeWaterLevelUI(0);
    }

    private void OnDisable()
    {
        UnbindCurrentTank();
    }

    public void ShowForTank(WaterTankInteractable tank)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        BindTank(tank);

        if (UIManager.Instance != null)
        {
            if (!IsOpen)
                UIManager.Instance.ShowUI(Type);
            else
                RefreshBoundTankUI();
        }
        else
        {
            Show();
        }

        Debug.Log("[WaterTank] 워터탱크 UI 표시 호출");
    }

    private void BindTank(WaterTankInteractable tank)
    {
        if (currentTank == tank)
        {
            RefreshBoundTankUI();
            return;
        }

        UnbindCurrentTank();
        currentTank = tank;

        if (currentTank != null)
            currentTank.OnWaterLevelChanged += ChangeWaterLevelUI;

        RefreshBoundTankUI();
    }

    private void UnbindCurrentTank()
    {
        if (currentTank != null)
            currentTank.OnWaterLevelChanged -= ChangeWaterLevelUI;

        currentTank = null;
    }

    private void RefreshBoundTankUI()
    {
        ChangeWaterLevelUI(currentTank != null ? currentTank.WaterLevel : 0);
    }

    protected override void OnShow()
    {
        backgroundImage.gameObject.SetActive(true);
        RefreshBoundTankUI();
        SoundManager.I?.PlayOpenBox();
    }

    protected override void OnHide()
    {
        SoundManager.I?.PlayCloseCraftingTable();
        backgroundImage.gameObject.SetActive(false);
    }

    private void OnStoreWaterButtonClicked()
    {
        currentTank?.RequestStoreWaterFromLocalPlayer();
        SoundManager.I?.PlayWater();
    }

    private void OnRetrieveWaterButtonClicked()
    {
        Debug.Log("[WaterTankUI] 물 회수 버튼 클릭");
        currentTank?.RequestRetrieveWaterFromLocalPlayer();
        SoundManager.I?.PlayWater();
    }

    private void ChangeWaterLevelUI(int value)
    {
        if (waterLevelText != null)
            waterLevelText.text = $"{value}%";

        if (waterTankSlider != null)
            waterTankSlider.value = value;
    }

    private void OnstoreContaminatedWaterButtonClicked()
    {
        ToastMessageUI.Instance.Show("온실 업그레이드 컨텐츠 추가 이후 사용 가능합니다.");
    }
}
