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

    public void Open(FarmBed plot, PlayerController player, int focusCellIndex = -1)
    {
       if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        Unbind();

        currentPlot = plot;
        currentPlayer = player;


        if (inventoryItemData != null)
            inventoryItemData.OnInventoryChanged += RefreshSeeds;

        if (currentPlot != null)
            currentPlot.OnChanged += RefreshGrid;

        RefreshSeeds();
        RefreshGrid();

        if (focusCellIndex >= 0)
            SetFocusCell(focusCellIndex);

        UIManager.Instance.ShowUI(UIType.Farm);
    }

    public override void Hide()
    {
        Unbind();
        infoPanel.Hide();
        base.Hide();
        UIManager.Instance.HideUI(UIType.Farm);
    }

    private void Unbind()
    {
        if (inventoryItemData != null)
            inventoryItemData.OnInventoryChanged -= RefreshSeeds;

        if (currentPlot != null)
            currentPlot.OnChanged -= RefreshGrid;

        //inventoryItemData = null;
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

    private void SetFocusCell(int cellIndex)
    {
        if (currentPlot == null) return;
        if (cellIndex < 0 || cellIndex >= currentPlot.SlotCount) return;

        currentCellIndex = cellIndex;
        var cell = currentPlot.Slots[cellIndex];
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

        // 1) 씨앗 -> 작물 row 찾기
        if (!currentPlot.FarmDB.TryGetBySeedId(seedItemId, out var cropRow))
        {
            ToastMessageUI.Instance?.Show("이 씨앗은 심을 수 없어요.");
            return;
        }

        // 2) 심을 수 있는지 체크
        if (!currentPlot.CanPlant(cellIndex, cropRow.cropId))
        {
            ToastMessageUI.Instance?.Show("여기에는 심을 수 없어요.");
            return;
        }

        // 3) 인벤에서 씨앗 차감
        if (!inventoryItemData.RemoveItem(seedItemId, 1))
        {
            ToastMessageUI.Instance?.Show("씨앗이 부족합니다.");
            return;
        }

        // 4) 심기
        currentPlot.Plant(cellIndex, cropRow.cropId);

        ToastMessageUI.Instance?.Show($"{cropRow.cropName} 심기 완료!");
    }
}
