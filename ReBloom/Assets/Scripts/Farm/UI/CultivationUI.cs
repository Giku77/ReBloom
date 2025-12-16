using UnityEngine;

public class CultivationUI : UIBase
{
    [Header("Refs")]
    [SerializeField] private SeedListPanel seedListPanel;
    [SerializeField] private CultivationInfoPanel infoPanel;
    [SerializeField] private CultivationCellSlotUI cellSlotUI;

    [Header("Data")]
    [SerializeField] private InventoryItemData inventoryItemData;

    private CultivationMachine currentMachine;
    private PlayerController currentPlayer;

    private bool _infoPanelBound;

    public void Open(CultivationMachine machine, PlayerController player)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        Unbind();

        currentMachine = machine;
        currentPlayer = player;

        if (inventoryItemData != null)
            inventoryItemData.OnInventoryChanged += RefreshSeeds;

        if (currentMachine != null)
            currentMachine.OnChanged += OnMachineChanged;

        BindInfoPanelEvents();

        RefreshSeeds();

        if (infoPanel != null)
        {
            infoPanel.Show(currentMachine);
        }

        if (cellSlotUI != null)
        {
            cellSlotUI.Bind(
                currentMachine,
                onClick: () => infoPanel.Show(currentMachine),
                onSeedDrop: OnSeedDropped
            );
        }

        UIManager.Instance.ShowUI(UIType.Cultivation);
    }

    public override void Hide()
    {
        Unbind();
        if (infoPanel != null) infoPanel.Hide();
        base.Hide();
        UIManager.Instance.HideUI(UIType.Cultivation);
    }

    private void Unbind()
    {
        if (inventoryItemData != null)
            inventoryItemData.OnInventoryChanged -= RefreshSeeds;

        if (currentMachine != null)
            currentMachine.OnChanged -= OnMachineChanged;

        if (infoPanel != null && _infoPanelBound)
        {
            infoPanel.OnCollectClicked -= HandleCollectClicked;
            _infoPanelBound = false;
        }

        currentMachine = null;
        currentPlayer = null;
    }

    private void OnMachineChanged()
    {
        if (cellSlotUI != null) cellSlotUI.Refresh();
        if (infoPanel != null && infoPanel.gameObject.activeSelf)
            infoPanel.Refresh(currentMachine);
    }

    private void BindInfoPanelEvents()
    {
        if (infoPanel == null) return;
        if (_infoPanelBound) return;

        infoPanel.OnCollectClicked += HandleCollectClicked; // Action 형태
        _infoPanelBound = true;
    }

    private void RefreshSeeds()
    {
        if (inventoryItemData == null || currentMachine == null) return;

        var farmDB = FarmPrefabProvider.I.FarmDB;
        var stacks = SeedStackBuilder.Build(inventoryItemData, farmDB);

        seedListPanel.Bind(stacks, OnSeedClicked);
    }

    private void OnSeedClicked(int seedItemId)
    {
        OnSeedDropped(seedItemId);
    }

    public void OnSeedDropped(int seedItemId)
    {
        if (inventoryItemData == null || currentMachine == null) return;

        if (!currentMachine.CanStart(seedItemId, out var reason))
        {
            ToastMessageUI.Instance?.Show(reason ?? "투입할 수 없습니다.");
            return;
        }

        if (!inventoryItemData.RemoveItem(seedItemId, 1))
        {
            ToastMessageUI.Instance?.Show("아이템이 부족합니다.");
            return;
        }

        if (!currentMachine.StartMachine(seedItemId))
        {
            inventoryItemData.AddItem(seedItemId, 1); // 롤백
            ToastMessageUI.Instance?.Show("배양 시작에 실패했습니다.");
            return;
        }

        ToastMessageUI.Instance?.Show("배양을 시작했습니다!");
        if (infoPanel != null) infoPanel.Refresh(currentMachine);
    }

    private void HandleCollectClicked()
    {
        if (currentMachine == null || currentPlayer == null) return;

        if (!currentMachine.CanCollect())
        {
            ToastMessageUI.Instance?.Show("수거할 수 없습니다.");
            return;
        }

        if (!currentMachine.Collect(currentPlayer, out var reason))
        {
            ToastMessageUI.Instance?.Show(reason);
            return;
        }

        ToastMessageUI.Instance?.Show("수거 완료!");
        if (infoPanel != null) infoPanel.Refresh(currentMachine);
    }
}
