using Unity.VisualScripting;
using UnityEngine;

public class FarmUI : UIBase
{
    [Header("Refs")]
    [SerializeField] private SeedListPanel seedListPanel;
    [SerializeField] private FarmGridPanel gridPanel;
    [SerializeField] private FarmCellInfoPanel infoPanel;

    [SerializeField] private InventoryItemData inventoryItemData;
    private FarmBed currentPlot;
    private PlayerController currentPlayer;

    private int currentCellIndex = -1;
    private System.Action _plotChangedHandler;

    public void Open(FarmBed plot, PlayerController player, int focusCellIndex = -1)
    {
       if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        Unbind();

        currentPlot = plot;
        currentPlayer = player;


        //if (inventoryItemData != null)
        //    inventoryItemData.OnInventoryChanged += RefreshSeeds;

        if (currentPlot != null)
        {
            _plotChangedHandler = OnPlotChanged;
            currentPlot.OnChanged += _plotChangedHandler;
        }
           
        BindInfoPanelEvents();

        if (infoPanel != null)
            infoPanel.Hide();

        RefreshSeeds();
        RefreshGrid();

        if (focusCellIndex >= 0)
            SetFocusCell(focusCellIndex);

        UIManager.Instance.ShowUI(UIType.Farm);
    }

    public override void Hide()
    {
        Debug.Log("[FarmUI] Hide called.");
        var tpsCam = Camera.main.GetComponent<ThirdPersonCamera>();
        tpsCam?.ExitTopDown();

        Unbind();
        infoPanel.Hide();
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
        // if (inventoryItemData != null)
        // inventoryItemData.OnInventoryChanged -= RefreshSeeds;

        if (currentPlot != null && _plotChangedHandler != null)
            currentPlot.OnChanged -= _plotChangedHandler;

        if (infoPanel != null && _infoPanelBound)
        {
            infoPanel.OnWaterClicked -= HandleWaterClicked;
            infoPanel.OnHarvestClicked -= HandleHarvestClicked;
            infoPanel.OnUprootClicked -= HandleUprootClicked;
            _infoPanelBound = false;
        }

        _plotChangedHandler = null;

        currentPlot = null;
        currentPlayer = null;
        currentCellIndex = -1;
    }

    private void RefreshSeeds()
    {
        if (inventoryItemData == null || currentPlot == null) return;

        // 씨앗 스택 집계(Seed_ID 기준)
        var stacks = SeedStackBuilder.Build(inventoryItemData, currentPlot.FarmDB);

        seedListPanel.Bind(stacks, OnSeedClicked); 
    }

    private void RefreshGrid()
    {
        if (currentPlot == null) return;

        gridPanel.Bind(currentPlot, OnCellClicked, OnSeedDroppedToCell);
    }

    public void OnCellClicked(int cellIndex)
    {
        SetFocusCell(cellIndex);
    }

    private bool _infoPanelBound;
    private void BindInfoPanelEvents()
    {
        if (infoPanel == null) return;
        if (_infoPanelBound) return;

        infoPanel.OnWaterClicked += HandleWaterClicked;
        infoPanel.OnHarvestClicked += HandleHarvestClicked;
        infoPanel.OnUprootClicked += HandleUprootClicked;

        _infoPanelBound = true;
    }

    private void HandleWaterClicked(int cellIndex)
    {
        if (currentPlot == null) return;

        if (!currentPlot.CanWater(cellIndex))
        {
            ToastMessageUI.Instance?.Show("지금은 물을 줄 수 없습니다.");
            return;
        }

        currentPlot.Water(cellIndex);

        RefreshGrid();
        infoPanel.Refresh();
        ToastMessageUI.Instance?.Show("물을 주었습니다.");
    }

    private void HandleHarvestClicked(int cellIndex)
    {
        if (currentPlot == null || currentPlayer == null) return;

        if (!currentPlot.CanHarvest(cellIndex))
        {
            ToastMessageUI.Instance?.Show("아직 수확할 수 없습니다.");
            return;
        }

        currentPlot.Harvest(cellIndex, currentPlayer);

        RefreshGrid();
        infoPanel.Hide(); 
        ToastMessageUI.Instance?.Show("수확 완료!");
    }

    private void HandleUprootClicked(int cellIndex)
    {
        if (currentPlot == null) return;

        var slot = currentPlot.GetSlot(cellIndex);
        if (slot == null || slot.state == CropSlotState.Empty)
            return;

        currentPlot.Uproot(cellIndex);

        RefreshGrid();
        infoPanel.Hide();
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
            ToastMessageUI.Instance?.Show("자라고 있는 씨앗이 없습니다.");
            return;
        }

        infoPanel.Show(cellIndex, currentPlot);

        // 원하면 하이라이트도 UI에서 제어 가능
        // gridPanel.SetSelected(cellIndex);
    }

    private void OnSeedClicked(int seedItemId)
    {
        if (currentCellIndex < 0)
        {
            ToastMessageUI.Instance?.Show("심을 칸을 먼저 선택해 주세요.");
            return;
        }

        OnSeedDroppedToCell(seedItemId, currentCellIndex);
    }

    public void OnSeedDroppedToCell(int seedItemId, int cellIndex)
    {
        if (inventoryItemData == null || currentPlot == null) return;

        if (!currentPlot.FarmDB.TryGetBySeedId(seedItemId, out var cropRow))
        {
            ToastMessageUI.Instance?.Show("이 씨앗은 심을 수 없습니다.");
            return;
        }

        if (!currentPlot.CanPlant(cellIndex, cropRow.cropId))
        {
            ToastMessageUI.Instance?.Show("여기에는 심을 수 없습니다.");
            return;
        }

        if (!inventoryItemData.TryRemoveItem(seedItemId, 1))
        {
            ToastMessageUI.Instance?.Show("씨앗이 부족합니다.");
            return;
        }

        currentPlot.Plant(cellIndex, cropRow.cropId);

        ToastMessageUI.Instance?.Show($"{cropRow.cropName} 심기 완료!");
    }
}
