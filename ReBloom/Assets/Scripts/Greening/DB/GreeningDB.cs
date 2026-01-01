using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGDatabase;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GreeningDB
{
    private readonly List<GreeningRow> _sorted = new();
    public bool IsLoaded { get; private set; }
    public event System.Action OnLoadComplete;

    public IReadOnlyList<GreeningRow> SortedRows => _sorted;

    public async void LoadFromBG(bool autoComputeMinGreening = true)
    {
        IsLoaded = false;
        _sorted.Clear();

        var meta = BGRepo.I.GetMeta("Greening");
        if (meta == null)
        {
            Debug.LogError("[GreeningDB] Meta 'Greening' not found.");
            return;
        }

        var entities = meta.EntitiesToList();
        foreach (var e in entities)
        {
            string planet = SafeKey(e.Get<string>("PlanetObject"));
            string animal = SafeKey(e.Get<string>("AnimalObject"));
            string insect = SafeKey(e.Get<string>("InsectObject"));

            byte tr = (byte)e.Get<int>("TerrainRcolor");
            byte tg = (byte)e.Get<int>("TerrainGcolor");
            byte tb = (byte)e.Get<int>("TerrainBcolor");

            byte fr = (byte)e.Get<int>("FogRcolor");
            byte fg = (byte)e.Get<int>("FogGcolor");
            byte fb = (byte)e.Get<int>("FogBcolor");

            var row = new GreeningRow
            {
                greeningId = e.Get<int>("GreeningID"),
                planetKey = planet,
                animalKey = animal,
                insectKey = insect,
                terrainColor = new Color32(tr, tg, tb, 255),
                fogColor = new Color32(fr, fg, fb, 255),
                minGreening = 0f, // 아래에서 채움
            };

            _sorted.Add(row);
        }

        _sorted.Sort((a, b) => a.greeningId.CompareTo(b.greeningId));

        if (autoComputeMinGreening)
        {
            for (int i = 0; i < _sorted.Count; i++)
                _sorted[i].minGreening = i * 5f;
        }

        await UniTask.Yield();

        IsLoaded = true;
        OnLoadComplete?.Invoke();
        Debug.Log($"[GreeningDB] Load complete. rows={_sorted.Count}");
    }

    public int GetStageIndex(float greening)
    {
        if (_sorted.Count == 0) return -1;

        for (int i = _sorted.Count - 1; i >= 0; i--)
        {
            if (greening >= _sorted[i].minGreening)
                return i;
        }
        return 0;
    }

    public GreeningRow GetRowByGreening(float greening)
    {
        int idx = GetStageIndex(greening);
        if (idx < 0) return null;
        return _sorted[idx];
    }

    public (string planet, string animal, string insect, GreeningRow currentRow) GetEffectiveState(float greening)
    {
        int idx = GetStageIndex(greening);
        if (idx < 0) return ("0", "0", "0", null);

        string p = "0", a = "0", i = "0";
        for (int k = 0; k <= idx; k++)
        {
            var r = _sorted[k];
            if (!IsZero(r.planetKey)) p = r.planetKey;
            if (!IsZero(r.animalKey)) a = r.animalKey;
            if (!IsZero(r.insectKey)) i = r.insectKey;
        }

        return (p, a, i, _sorted[idx]);
    }

    private static bool IsZero(string s) => string.IsNullOrWhiteSpace(s) || s.Trim() == "0";
    private static string SafeKey(string s) => string.IsNullOrWhiteSpace(s) ? "0" : s.Trim();
}
