using System.Collections.Generic;
using UnityEngine;

public class GreenhouseUpgradeUI : UIBase
{
    [Header("Grid")]
    [SerializeField] private Transform slotRoot;          // GridLayoutGroup 붙은 부모
    [SerializeField] private UpgradeNodeUI slotPrefab;

    private IItemContainer _inventory;

    private GreenhouseContext _ctx;
    private GreenhouseUpgradeState _state;
    private GreenhouseUpgradeDB _db;

    private readonly List<UpgradeNodeUI> _slots = new();
    private readonly List<GreenhouseUpgradeRowData> _rows = new();

    public void Open(GreenhouseContext ctx, GreenhouseUpgradeState state, GreenhouseUpgradeDB db, IItemContainer inventory)
    {
        _ctx = ctx;
        _state = state;
        _db = db;
        _inventory = inventory;
        BuildFromDB();   // 슬롯 생성/배치
        RefreshAll();    // 잠금/버튼/비용 표시 갱신
        UIManager.Instance.ShowUI(UIType.FarmUpgrade);
    }

    private void BuildFromDB()
    {
        _rows.Clear();

        AddSortRows(1);
        AddSortRows(2);
        AddSortRows(3);

        void AddSortRows(int sort)
        {
            var list = _db.GetRowsBySort(sort);
            for (int i = 0; i < list.Count; i++)
                _rows.Add(list[i]);
        }

        // 2) 슬롯 수 맞추기
        EnsureSlotCount(_rows.Count);

        // 3) 각 슬롯에 row 바인딩
        for (int i = 0; i < _rows.Count; i++)
            _slots[i].Bind(_rows[i], OnClickUpgrade);
    }

    private void EnsureSlotCount(int count)
    {
        while (_slots.Count < count)
        {
            var inst = Instantiate(slotPrefab, slotRoot);
            _slots.Add(inst);
        }

        for (int i = _slots.Count - 1; i >= count; i--)
        {
            Destroy(_slots[i].gameObject);
            _slots.RemoveAt(i);
        }
    }

    public void RefreshAll()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];

            bool completed = GreenhouseUpgradeService.IsCompleted(_state, row);
            bool unlocked  = GreenhouseUpgradeService.IsUnlocked(_state, row);
            bool affordable = unlocked && CanAfford(row); // TODO 인벤 체크

            _slots[i].RefreshState(_state, row, completed, unlocked, affordable);
        }
    }

    private bool CanAfford(GreenhouseUpgradeRowData row)
    {
        // TODO: 인벤에서 재료 충분한지 체크
        return true;
    }

    private void OnClickUpgrade(int upgradeId)
    {
        if (!_db.TryGet(upgradeId, out var row)) return;


        if (GreenhouseUpgradeService.Purchase(_ctx, _state, row, _inventory))
        {
            RefreshAll();
        }
        else
        {
            ToastMessageUI.Instance?.Show("재료가 부족합니다.");
        }
    }

}
