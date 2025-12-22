using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class WaterTankUI : UIBase
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameInventory inventoryItemData;

    [Header("UI")]
    [SerializeField] private Button storeWaterButton;
    [SerializeField] private Button retrieveWaterButton;
    [SerializeField] private Button storeContaminatedWaterButton;
    [SerializeField] private TextMeshProUGUI waterLevelText;
    [SerializeField] private Slider waterTankSlider;
    //[SerializeField] private GameObject StoreRainTextBox;

    protected override void Awake()
    {
        base.Awake();

        storeWaterButton.onClick.AddListener(OnStoreWaterButtonClicked);
        retrieveWaterButton.onClick.AddListener(OnRetrieveWaterButtonClicked);
        storeContaminatedWaterButton.onClick.AddListener(OnstoreContaminatedWaterButtonClicked);

        waterTankSlider.value = 0f;

        //StoreRainTextBox?.SetActive(false);
    }

    private void OnEnable()
    {
        WaterTankManager.OnWaterLevelChange += ChangeWaterLevelUI;
    }

    private void OnDisable()
    {
        WaterTankManager.OnWaterLevelChange -= ChangeWaterLevelUI;
    }

    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;
        UIManager.Instance?.ToggleUI(Type);
        Debug.Log("[WaterTank] 워터탱크 UI 토클 호출");

        if (WaterTankService.I?.Manager != null)
            ChangeWaterLevelUI(WaterTankService.I.Manager.WaterLevel);

        //if (waterTankManager.isRaining)
            //StoreRainTextBox.SetActive(true);
    }

    protected override void OnShow()
    {
        backgroundImage.gameObject.SetActive(true);

        if (WaterTankService.I?.Manager != null)
            ChangeWaterLevelUI(WaterTankService.I.Manager.WaterLevel);

        //if (waterTankManager.isRaining)
        //    StoreRainTextBox.SetActive(true);
    }

    protected override void OnHide()
    {
        //if (waterTankManager.isRaining)
        //    StoreRainTextBox.SetActive(false);

        backgroundImage.gameObject.SetActive(false);
    }

    private void OnStoreWaterButtonClicked()
    {
        WaterTankService.I?.Manager.StoreWater();
    }

    private void OnRetrieveWaterButtonClicked()
    {
        Debug.Log("[WaterTankUI] 물 회수 버튼 클릭");
        WaterTankService.I?.Manager.RetrieveWater();
    }

    private void ChangeWaterLevelUI(int value)
    {
        if (waterLevelText != null)
        {
            waterLevelText.text = $"{value}%";
        }

        if (waterTankSlider != null)
        { 
            waterTankSlider.value = value;
        }
    }

    private void OnstoreContaminatedWaterButtonClicked()
    {
        ToastMessageUI.Instance.Show("온실 업그레이드 컨텐츠 추가 이후 사용 가능합니다.");
    }
}
