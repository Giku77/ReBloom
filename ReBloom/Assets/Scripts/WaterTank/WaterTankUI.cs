using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class WaterTankUI : UIBase
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private InventoryItemData inventoryItemData;
    public WaterTankManager waterTankManager;

    [Header("UI")]
    [SerializeField] private Button storeWaterButton;
    [SerializeField] private Button retrieveWaterButton;
    [SerializeField] private Button storeContaminatedWaterButton;
    [SerializeField] private TextMeshProUGUI waterLevelText;
    //[SerializeField] private GameObject StoreRainTextBox;

    protected override void Awake()
    {
        base.Awake();

        waterTankManager = new WaterTankManager(inventoryItemData);

        storeWaterButton.onClick.AddListener(OnStoreWaterButtonClicked);
        retrieveWaterButton.onClick.AddListener(OnRetrieveWaterButtonClicked);
        storeContaminatedWaterButton.onClick.AddListener(OnstoreContaminatedWaterButtonClicked);

        //StoreRainTextBox?.SetActive(false);
    }

    private void OnEnable()
    {
        WaterTankManager.OnWaterLevelChange += ChangeWaterLevelTextUI;
    }

    private void OnDisable()
    {
        WaterTankManager.OnWaterLevelChange -= ChangeWaterLevelTextUI;
    }

    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;
        UIManager.Instance?.ToggleUI(Type);
        Debug.Log("[WaterTank] 워터탱크 UI 토클 호출");

        if (waterTankManager != null)
            ChangeWaterLevelTextUI(waterTankManager.WaterLevel);

        //if (waterTankManager.isRaining)
            //StoreRainTextBox.SetActive(true);
    }

    protected override void OnShow()
    {
        backgroundImage.gameObject.SetActive(true);

        if (waterTankManager != null)
            ChangeWaterLevelTextUI(waterTankManager.WaterLevel);

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
        waterTankManager?.StoreWater();
    }

    private void OnRetrieveWaterButtonClicked()
    {
        Debug.Log("[WaterTankUI] 물 회수 버튼 클릭");
        waterTankManager?.RetrieveWater();
    }

    private void ChangeWaterLevelTextUI(int value)
    {
        if (waterLevelText == null) return;

        waterLevelText.text = $"{value}%";
    }

    private void OnstoreContaminatedWaterButtonClicked()
    {
        ToastMessageUI.Instance.Show("온실 업그레이드 컨텐츠 추가 이후 사용 가능합니다.");
    }
}
