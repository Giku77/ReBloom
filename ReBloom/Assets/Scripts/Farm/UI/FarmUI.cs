using UnityEngine;

public class FarmUI : UIBase
{
    [Header("Refs")]
    [SerializeField] private SeedListPanel seedListPanel;
    [SerializeField] private FarmGridPanel gridPanel;
    [SerializeField] private FarmCellInfoPanel infoPanel;

    private InventoryItemData inventoryItemData;
    private FarmBed currentPlot;
    private PlayerController currentPlayer;

    private int currentCellIndex = -1;
    private System.Action plotChangedHandler;
    private int hoveredIndex = -1;
    private bool infoPanelBound;

    public void Open(FarmBed plot, PlayerController player, int focusCellIndex = -1)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        Unbind();

        currentPlot = plot;
        currentPlayer = player;
        inventoryItemData = currentPlayer != null ? currentPlayer.Inventory?.Data : null;

        if (inventoryItemData != null)
            inventoryItemData.OnContainerChanged += RefreshSeeds;

        if (currentPlot != null)
        {
            plotChangedHandler = OnPlotChanged;
            currentPlot.OnChanged += plotChangedHandler;
        }

        BindInfoPanelEvents();

        if (infoPanel != null)
        {
            infoPanel.BindInventory(inventoryItemData);
            infoPanel.Hide();
        }

        RefreshSeeds();
        RefreshGrid();

        if (focusCellIndex >= 0)
            SetFocusCell(focusCellIndex);

        UIManager.Instance.ShowUI(UIType.Farm);
    }

    public override void Hide()
    {
        var tpsCam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
        tpsCam?.ExitTopDown();

        if (currentPlot != null && hoveredIndex != -1)
            currentPlot.SetSlotHighlighted(hoveredIndex, false);

        Unbind();
        infoPanel.Hide();
        hoveredIndex = -1;

        base.Hide();
        UIManager.Instance.HideUI(UIType.Farm);
    }

    private void OnPlotChanged()
    {
        RefreshGrid();
        if (infoPanel != null && infoPanel.gameObject.activeSelf)
            infoPanel.Refresh();
    }

    private void Unbind()
    {
        if (inventoryItemData != null)
            inventoryItemData.OnContainerChanged -= RefreshSeeds;

        if (currentPlot != null && plotChangedHandler != null)
            currentPlot.OnChanged -= plotChangedHandler;

        if (infoPanel != null && infoPanelBound)
        {
            infoPanel.OnWaterClicked -= HandleWaterClicked;
            infoPanel.OnHarvestClicked -= HandleHarvestClicked;
            infoPanel.OnUprootClicked -= HandleUprootClicked;
            infoPanel.OnFertilizeClicked -= HandleFertilizeClicked;
            infoPanelBound = false;
        }

        plotChangedHandler = null;
        inventoryItemData = null;
        currentPlot = null;
        currentPlayer = null;
        currentCellIndex = -1;
    }

    private void RefreshSeeds()
    {
        if (inventoryItemData == null || currentPlot == null)
            return;

        var stacks = SeedStackBuilder.Build(inventoryItemData, currentPlot.FarmDB);
        seedListPanel.Bind(stacks, OnSeedClicked);
    }

    private void RefreshGrid()
    {
        if (currentPlot == null)
            return;

        gridPanel.Bind(currentPlot, OnCellClicked, OnSeedDroppedToCell, OnCellHoverChanged);
    }

    private void OnCellHoverChanged(int idx, bool enter)
    {
        if (currentPlot == null)
            return;

        if (hoveredIndex != -1 && hoveredIndex != idx)
            currentPlot.SetSlotHighlighted(hoveredIndex, false);

        if (enter)
        {
            currentPlot.SetSlotHighlighted(idx, true);
            hoveredIndex = idx;
        }
        else
        {
            currentPlot.SetSlotHighlighted(idx, false);
            if (hoveredIndex == idx)
                hoveredIndex = -1;
        }
    }

    public void OnCellClicked(int cellIndex)
    {
        SetFocusCell(cellIndex);
    }

    private void BindInfoPanelEvents()
    {
        if (infoPanel == null || infoPanelBound)
            return;

        infoPanel.OnWaterClicked += HandleWaterClicked;
        infoPanel.OnHarvestClicked += HandleHarvestClicked;
        infoPanel.OnUprootClicked += HandleUprootClicked;
        infoPanel.OnFertilizeClicked += HandleFertilizeClicked;
        infoPanelBound = true;
    }

    private void HandleWaterClicked(int cellIndex)
    {
        if (currentPlot == null)
            return;

        if (!currentPlot.CanWater(cellIndex))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("지금은 물을 줄 수 없습니다.");
            return;
        }

        if (!currentPlot.RequestWaterFromLocalPlayer(cellIndex))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("물이 부족합니다.");
            return;
        }

        SoundManager.I?.PlayWater();
        ToastMessageUI.Instance?.Show("물을 주었습니다.");
    }

    private void HandleHarvestClicked(int cellIndex)
    {
        if (currentPlot == null)
            return;

        if (!currentPlot.CanHarvest(cellIndex))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("아직 수확할 수 없습니다.");
            return;
        }

        if (!currentPlot.RequestHarvestFromLocalPlayer(cellIndex))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("수확할 수 없습니다.");
            return;
        }

        SoundManager.I?.PlayGetSeed();
        ToastMessageUI.Instance?.Show("수확 완료!");
    }

    private void HandleUprootClicked(int cellIndex)
    {
        if (currentPlot == null)
            return;

        var slot = currentPlot.GetSlot(cellIndex);
        if (slot == null || slot.state == CropSlotState.Empty)
            return;

        if (!currentPlot.RequestUprootFromLocalPlayer(cellIndex))
            return;

        SoundManager.I?.PlayGetSeed();
        ToastMessageUI.Instance?.Show("뽑았습니다.");
    }

    private void SetFocusCell(int cellIndex)
    {
        if (currentPlot == null) return;
        if (cellIndex < 0 || cellIndex >= currentPlot.SlotCount) return;

        currentCellIndex = cellIndex;
        var cell = currentPlot.Slots[cellIndex];
        if (cell == null || cell.state == CropSlotState.Empty)
        {
            infoPanel.Hide();
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("자라고 있는 씨앗이 없습니다.");
            return;
        }

        infoPanel.Show(cellIndex, currentPlot);
    }

    private void OnSeedClicked(int seedItemId)
    {
        if (currentCellIndex < 0)
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("심을 칸을 먼저 선택해 주세요.");
            return;
        }

        SoundManager.I?.PlayUIClick();
        OnSeedDroppedToCell(seedItemId, currentCellIndex);
    }

    public void OnSeedDroppedToCell(int seedItemId, int cellIndex)
    {
        if (inventoryItemData == null || currentPlot == null)
            return;

        if (!currentPlot.FarmDB.TryGetBySeedId(seedItemId, out var cropRow))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("이 씨앗은 심을 수 없습니다.");
            return;
        }

        if (!currentPlot.CanPlant(cellIndex, cropRow.cropId))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("여기에는 심을 수 없습니다.");
            return;
        }

        if (!inventoryItemData.HasItem(seedItemId, 1))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("씨앗이 부족합니다.");
            return;
        }

        if (!currentPlot.RequestPlantFromLocalPlayer(cellIndex, seedItemId))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("심을 수 없습니다.");
            return;
        }

        SoundManager.I?.PlaySeed();
        ToastMessageUI.Instance?.Show($"{cropRow.cropName} 심기 완료!");
    }

    private void HandleFertilizeClicked(int cellIndex)
    {
        if (currentPlot == null || inventoryItemData == null)
            return;

        if (inventoryItemData.GetItemCount(FarmConst.FertilizerItemId) <= 0)
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("비료가 부족합니다.");
            return;
        }

        if (!currentPlot.RequestFertilizeFromLocalPlayer(cellIndex))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("비료를 사용할 수 없습니다. (비료 부족/대상 아님)");
            return;
        }

        SoundManager.I?.PlayWater();
        ToastMessageUI.Instance?.Show("비료를 사용했습니다!");
    }
}
