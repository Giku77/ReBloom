using System;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGDatabase;
using UnityEngine;

/// <summary>
/// BGDatabase 테이블(예: SeedPurify / SeedPurify_Table)에서
/// UseItem(미확인 종자) -> (결과 종자, 확률) 목록을 로드해서 룰렛 롤을 제공.
/// </summary>
public class SeedPurifyDB
{
    public static SeedPurifyDB I { get; } = new SeedPurifyDB();

    // UseItemId(미확인 종자 ID) -> 룰렛 테이블
    private readonly Dictionary<int, SeedPurifyTable> _tables = new();

    public bool IsLoaded { get; private set; }

    /// <summary>
    /// BG 메타 이름 (BGRepo에서 보이는 Meta 이름)
    /// </summary>
    [SerializeField] private string metaName = "SeedPercent";

    // ----------------------------
    // Load
    // ----------------------------
    public void LoadFromBG(string overrideMetaName = null)
    {
        _tables.Clear();
        IsLoaded = false;

        string useMeta = string.IsNullOrEmpty(overrideMetaName) ? metaName : overrideMetaName;

        var meta = BGRepo.I.GetMeta(useMeta);
        if (meta == null)
        {
            Debug.LogError($"[SeedPurifyDB] Meta not found: {useMeta}");
            return;
        }

        foreach (var e in meta.EntitiesToList())
        {
            int resultId   = e.Get<int>("ResultID");
            string name    = SafeGetString(e, "ResultName");   // optional
            int useItemId  = e.Get<int>("UseItem");
            float percent  = e.Get<float>("Percent");          // 0~100 or weight

            if (!_tables.TryGetValue(useItemId, out var table))
            {
                table = new SeedPurifyTable(useItemId);
                _tables.Add(useItemId, table);
            }

            table.Add(new SeedPurifyEntry
            {
                ResultId = resultId,
                ResultName = name,
                Percent = percent
            });
        }

        // 정규화/정리
        foreach (var t in _tables.Values)
            t.Build();

        IsLoaded = true;
        Debug.Log($"[SeedPurifyDB] Loaded. UseItem tables = {_tables.Count}");
    }

    private static string SafeGetString(BGEntity entity, string fieldName)
    {
        try { return entity.Get<string>(fieldName); }
        catch { return string.Empty; }
    }

    // ----------------------------
    // Query
    // ----------------------------
    public bool TryGetTable(int useItemId, out SeedPurifyTable table)
        => _tables.TryGetValue(useItemId, out table);

    public IReadOnlyDictionary<int, SeedPurifyTable> GetAllTables()
        => _tables;

    /// <summary>
    /// (편의) 랜덤으로 결과 뽑기
    /// </summary>
    public int Roll(int useItemId, System.Random rng)
    {
        if (!_tables.TryGetValue(useItemId, out var t) || t.Entries.Count == 0)
            return 0;
        return t.Roll(rng);
    }
}

/// <summary>
/// UseItemId 하나에 대응하는 결과 룰렛 테이블
/// </summary>
[Serializable]
public class SeedPurifyTable
{
    public int UseItemId { get; }
    public List<SeedPurifyEntry> Entries { get; } = new();

    private float _totalWeight;

    public SeedPurifyTable(int useItemId)
    {
        UseItemId = useItemId;
    }

    public void Add(SeedPurifyEntry entry)
    {
        if (entry == null) return;
        // 0 이하는 무시(실수 방지)
        if (entry.Percent <= 0f) return;
        Entries.Add(entry);
    }

    /// <summary>
    /// 로드 후 한번 호출해서 totalWeight 계산/정리
    /// </summary>
    public void Build()
    {
        // 같은 ResultId 중복이 있을 수 있으면 합치고 싶을 때(선택)
        // 지금은 그냥 total만 계산
        _totalWeight = 0f;
        for (int i = 0; i < Entries.Count; i++)
            _totalWeight += Mathf.Max(0f, Entries[i].Percent);
    }

    /// <summary>
    /// percent를 "가중치"로 보고 룰렛 롤
    /// (percent 합이 100이 아니어도 동작)
    /// </summary>
    public int Roll(System.Random rng)
    {
        if (Entries.Count == 0) return 0;

        // Build() 안 했어도 동작하도록 안전장치
        if (_totalWeight <= 0f)
        {
            _totalWeight = Entries.Sum(e => Mathf.Max(0f, e.Percent));
            if (_totalWeight <= 0f) return 0;
        }

        double r = rng.NextDouble() * _totalWeight;
        float acc = 0f;

        for (int i = 0; i < Entries.Count; i++)
        {
            acc += Mathf.Max(0f, Entries[i].Percent);
            if (r <= acc)
                return Entries[i].ResultId;
        }

        // 부동소수 오차 대비
        return Entries[Entries.Count - 1].ResultId;
    }
}

/// <summary>
/// 테이블 1행에 해당하는 데이터
/// </summary>
[Serializable]
public class SeedPurifyEntry
{
    public int ResultId;
    public string ResultName;
    public float Percent; // weight
}
