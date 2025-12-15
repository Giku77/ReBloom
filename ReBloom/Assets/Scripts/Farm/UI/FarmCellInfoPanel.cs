using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FarmCellInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI remainText;
    [SerializeField] private TextMeshProUGUI waterText;

    [SerializeField] private Button waterBtn;
    [SerializeField] private Button harvestBtn;
    [SerializeField] private Button uprootBtn; // “파종(뽑기)” 같은 의미로 쓰면 됨

    private int currentIndex = -1;
    private FarmBed plot;

    public event Action<int> OnWaterClicked;
    public event Action<int> OnHarvestClicked;
    public event Action<int> OnUprootClicked;

    private void Awake()
    {
        if (waterBtn) waterBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnWaterClicked?.Invoke(currentIndex); });
        if (harvestBtn) harvestBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnHarvestClicked?.Invoke(currentIndex); });
        if (uprootBtn) uprootBtn.onClick.AddListener(() => { if (currentIndex >= 0) OnUprootClicked?.Invoke(currentIndex); });
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
                remainText.text = $"남은 시간 : {cell.stageTimer}초";
            if (waterText)
                waterText.text = $"필요한 물 : {cropData.stages[cell.stageIndex].needWater}회";
        }
        if (waterBtn) waterBtn.gameObject.SetActive(cell.state == CropSlotState.Growing);
        if (harvestBtn) harvestBtn.gameObject.SetActive(cell.state == CropSlotState.Mature);
        if (uprootBtn) uprootBtn.gameObject.SetActive(cell.state != CropSlotState.Empty);
    }
}
