using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FarmCellInfoPanel : MonoBehaviour
{
    [SerializeField] private Image cropIcon;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI remainText;
    [SerializeField] private TextMeshProUGUI fertilizerText;
    [SerializeField] private TextMeshProUGUI waterText;

    [SerializeField] private Button waterBtn;
    [SerializeField] private Button harvestBtn;
    [SerializeField] private Button uprootBtn; // “파종(뽑기)” 같은 의미로 쓰면 됨
    [SerializeField] private Button fertilizeBtn;
    [SerializeField] private Button closeBtn;

    private int currentIndex = -1;
    private FarmBed plot;

    public event Action<int> OnWaterClicked;
    public event Action<int> OnHarvestClicked;
    public event Action<int> OnUprootClicked;
    public event Action<int> OnFertilizeClicked;

    private float _nextRefreshTime;
    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + 1f; 
            Refresh();
        }
        if (Keyboard.current == null) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (plot == null || currentIndex < 0) return;

            var cell = plot.Slots[currentIndex];
            if (cell == null || cell.state != CropSlotState.Growing) return;

            if (!plot.FarmDB.TryGet(cell.cropId, out var cropData)) return;

            var stage = cropData.stages[cell.stageIndex];

            cell.stageTimer = stage.needTime;

            cell.wateredCount = stage.needWater;

            Refresh();
        }
    }

    private void Awake()
    {
        if (waterBtn) waterBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnWaterClicked?.Invoke(currentIndex); });
        if (harvestBtn) harvestBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnHarvestClicked?.Invoke(currentIndex); });
        if (uprootBtn) uprootBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnUprootClicked?.Invoke(currentIndex); });
        if (fertilizeBtn) fertilizeBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnFertilizeClicked?.Invoke(currentIndex); });
        if (closeBtn) closeBtn.onClick.AddListener(Hide);
    }

    private InventoryItemData inventory;

    public void BindInventory(InventoryItemData inv)
    {
        inventory = inv;
    }

    public void Show(int cellIndex, FarmBed plot)
    {
        this.currentIndex = cellIndex;
        this.plot = plot;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        currentIndex = -1;
        plot = null;
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (plot == null || currentIndex < 0) return;

        var cell = plot.Slots[currentIndex];
        if (plot.FarmDB.TryGet(cell.cropId, out var cropData))
        {
            // 수확물 아이콘 설정 (첫 번째 드랍 아이템)
            if (cropIcon != null)
            {
                if (cropData.drops != null && cropData.drops.Length > 0)
                {
                    var harvestItem = ItemDatabase.I.GetItem(cropData.drops[0].itemId);
                    if (harvestItem != null && harvestItem.icon != null)
                        cropIcon.sprite = harvestItem.icon;
                }
            }

            if (titleText)
                titleText.text = $"{cropData.cropName}";
            switch (cell.state)
            {
                case CropSlotState.Empty:
                    if (stateText)
                        stateText.text = "빈 칸";
                    break;
                case CropSlotState.Growing:
                    if (stateText)
                        stateText.text = $"성장 중 (단계 {cell.stageIndex + 1})";
                    break;
                case CropSlotState.Mature:
                    if (stateText)
                        stateText.text = "수확 가능";
                    break;
                case CropSlotState.Withered:
                    if (stateText)
                        stateText.text = "시듦";
                    break;
            }
            if (remainText)
            {
                if (cell.state == CropSlotState.Growing)
                {
                    var stage = cropData.stages[cell.stageIndex];
                    float remainTime = stage.needTime - cell.stageTimer;
                    remainText.text = $"남은 시간 : {Mathf.CeilToInt(remainTime)}초";
                }
                else
                {
                    remainText.text = $"남은 시간 : -";
                }
            }
            if (waterText)
            {
                if (cell.state == CropSlotState.Growing)
                {
                    var stage = cropData.stages[cell.stageIndex];
                    waterText.text = $"물 필요량 : {cell.wateredCount} / {stage.needWater}";
                }
                else
                {
                    waterText.text = $"물 필요량 : -";
                }
            }
            if (fertilizerText)
            {
                if (cell.state == CropSlotState.Growing)
                {
                    fertilizerText.text = $"비료 시간 : {Mathf.CeilToInt(cell.fertilizerRemain)}초";
                }
                else
                {
                    fertilizerText.text = $"비료 시간 : -";
                }
            }
        }
        if (waterBtn) waterBtn.gameObject.SetActive(cell.state == CropSlotState.Growing);
        if (harvestBtn) harvestBtn.gameObject.SetActive(cell.state == CropSlotState.Mature);
        if (uprootBtn) uprootBtn.gameObject.SetActive(cell.state != CropSlotState.Empty);
        bool hasFertilizer = inventory != null && inventory.GetItemCount(FarmConst.FertilizerItemId) > 0;
        bool canFertilize = cell.state == CropSlotState.Growing;

        if (fertilizeBtn)
        {
            fertilizeBtn.gameObject.SetActive(canFertilize && hasFertilizer);
        }
    }
}
