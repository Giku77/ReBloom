using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGDatabase;

public class GreenhouseUpgradeDB
{
    private readonly Dictionary<int, GreenhouseUpgradeRowData> _byId = new();
    private readonly Dictionary<int, List<GreenhouseUpgradeRowData>> _bySort = new();

    public void LoadFromBG(string metaName = "FarmUpgrade")
    {
        _byId.Clear();
        _bySort.Clear();

        var meta = BGRepo.I.GetMeta(metaName);

        foreach (var e in meta.EntitiesToList())
        {
            var row = new GreenhouseUpgradeRowData
            {
                upgradeId     = e.Get<int>("Upgrade_ID"),
                upgradeName   = e.Get<string>("Upgrade_Name"),

                sort          = e.Get<int>("Upgrade_Sort"),
                grade         = e.Get<int>("Upgrade_Grade"),

                function      = e.Get<int>("Upgrade_Function"),
                isApplyNewArc = e.Get<int>("IsApply_New_Arc") == 1,

                activePrefab1 = e.Get<string>("Active_Prefab1"),
                activePrefab2 = e.Get<string>("Active_Prefab2"),
                activePrefab3 = e.Get<string>("Active_Prefab3"),

                needItem1     = e.Get<int>("Need_Item1"),
                needCount1    = e.Get<int>("Need_Item_C1"),
                needItem2     = e.Get<int>("Need_Item2"),
                needCount2    = e.Get<int>("Need_Item_C2"),
            };

            _byId[row.upgradeId] = row;

            if (!_bySort.TryGetValue(row.sort, out var list))
            {
                list = new List<GreenhouseUpgradeRowData>();
                _bySort.Add(row.sort, list);
            }
            list.Add(row);
        }

        // sort별 grade 정렬
        foreach (var kv in _bySort)
            kv.Value.Sort((a, b) => a.grade.CompareTo(b.grade));

        Debug.Log($"[GreenhouseUpgradeDB] Loaded: {_byId.Count} rows (meta={metaName})");
    }

    public bool TryGet(int upgradeId, out GreenhouseUpgradeRowData row)
        => _byId.TryGetValue(upgradeId, out row);

    public IReadOnlyList<GreenhouseUpgradeRowData> GetRowsBySort(int sort)
    {
        if (_bySort.TryGetValue(sort, out var list)) return list;
        return System.Array.Empty<GreenhouseUpgradeRowData>();
    }

    public IEnumerable<int> GetAllSorts()
        => _bySort.Keys.OrderBy(x => x);
}
