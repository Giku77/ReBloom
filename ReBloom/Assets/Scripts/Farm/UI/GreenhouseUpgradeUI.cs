using System.Collections.Generic;
using UnityEngine;

public class GreenhouseUpgradeUI : UIBase
{
    [Header("Grid")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private UpgradeNodeUI slotPrefab;

    private InventoryItemData inventoryItemData;
    private GreenhouseContext greenhouseContext;
    private GreenhouseUpgradeDB upgradeDB;

    private readonly List<UpgradeNodeUI> slots = new();
    private readonly List<GreenhouseUpgradeRowData> rows = new();

    public void Open(GreenhouseContext ctx, GreenhouseUpgradeDB db, InventoryItemData inventory)
    {
        bool wasOpen = IsOpen;

        Unbind();

        greenhouseContext = ctx;
        upgradeDB = db;
        inventoryItemData = inventory;

        if (greenhouseContext != null)
            greenhouseContext.OnUpgradeStateChanged += RefreshAll;

        if (inventoryItemData != null)
            inventoryItemData.OnContainerChanged += RefreshAll;

        BuildFromDB();
        RefreshAll();
        UIManager.Instance.ShowUI(UIType.FarmUpgrade);

        if (!wasOpen)
            SoundManager.I?.PlayTvOn();
    }

    public override void Hide()
    {
        bool wasOpen = IsOpen;

        Unbind();
        base.Hide();
        UIManager.Instance.HideUI(UIType.FarmUpgrade);

        if (wasOpen)
            SoundManager.I?.PlayTvOff();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (greenhouseContext != null)
            greenhouseContext.OnUpgradeStateChanged -= RefreshAll;

        if (inventoryItemData != null)
            inventoryItemData.OnContainerChanged -= RefreshAll;

        greenhouseContext = null;
        inventoryItemData = null;
        upgradeDB = null;
    }

    private void BuildFromDB()
    {
        rows.Clear();
        if (upgradeDB == null)
            return;

        AddSortRows(1);
        AddSortRows(2);
        AddSortRows(3);

        EnsureSlotCount(rows.Count);

        for (int i = 0; i < rows.Count; i++)
            slots[i].Bind(rows[i], OnClickUpgrade);

        void AddSortRows(int sort)
        {
            var list = upgradeDB.GetRowsBySort(sort);
            for (int i = 0; i < list.Count; i++)
                rows.Add(list[i]);
        }
    }

    private void EnsureSlotCount(int count)
    {
        while (slots.Count < count)
        {
            var inst = Instantiate(slotPrefab, slotRoot);
            slots.Add(inst);
        }

        for (int i = slots.Count - 1; i >= count; i--)
        {
            Destroy(slots[i].gameObject);
            slots.RemoveAt(i);
        }
    }

    public void RefreshAll()
    {
        if (greenhouseContext == null || upgradeDB == null)
            return;

        var state = greenhouseContext.GetRuntimeStateSnapshot();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            bool completed = GreenhouseUpgradeService.IsCompleted(state, row);
            bool unlocked = GreenhouseUpgradeService.IsUnlocked(state, row);
            bool affordable = unlocked && CanAfford(state, row);
            slots[i].RefreshState(state, row, completed, unlocked, affordable);
        }
    }

    private bool CanAfford(GreenhouseUpgradeState state, GreenhouseUpgradeRowData row)
    {
        return inventoryItemData != null && GreenhouseUpgradeService.CanPurchase(state, row, inventoryItemData);
    }

    private void OnClickUpgrade(int upgradeId)
    {
        if (greenhouseContext == null || upgradeDB == null || !upgradeDB.TryGet(upgradeId, out var row))
            return;

        var state = greenhouseContext.GetRuntimeStateSnapshot();
        if (!GreenhouseUpgradeService.IsUnlocked(state, row))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("선행 업그레이드를 먼저 해금하세요.");
            return;
        }

        if (!GreenhouseUpgradeService.CanPurchase(state, row, inventoryItemData))
        {
            SoundManager.I?.PlayError();
            ToastMessageUI.Instance?.Show("재료가 부족합니다.");
            return;
        }

        if (greenhouseContext.RequestPurchaseFromLocalPlayer(upgradeId))
        {
            RefreshAll();
            return;
        }

        SoundManager.I?.PlayError();
        ToastMessageUI.Instance?.Show("재료가 부족합니다.");
    }
}