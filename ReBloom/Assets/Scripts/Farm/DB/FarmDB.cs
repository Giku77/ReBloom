using UnityEngine;
using System;
using System.Collections.Generic;
using BansheeGz.BGDatabase;

[Serializable]
public class FarmCropRowData
{
    public int cropId;          
    public string cropName;     
    public int seedItemId;     

    // 성장 단계 (Seed / Seedling / MidGrowth / Fruitling)
    public FarmGrowthStageData[] stages;

    // 수확/드랍 결과 (Item1/2는 보장, Item3는 확률)
    public FarmHarvestDropData[] drops;
}

[Serializable]
public class FarmGrowthStageData
{
    public string prefabKey;    
    public int needTime;        
    public int needWater;      
}

[Serializable]
public class FarmHarvestDropData
{
    public int itemId;         
    public int count;           
    public float rate;         
}


public class FarmDB
{
    private readonly Dictionary<int, FarmCropRowData> _crops = new();

    // BG Meta 이름은 프로젝트에서 쓰는 이름으로 바꿔줘.
    // 예: "Farming_Crops" / "Crops" / 너가 실제로 만든 테이블명
    public void LoadFromBG(string metaName = "Farm")
    {
        _crops.Clear();

        var meta = BGRepo.I.GetMeta(metaName);

        foreach (var e in meta.EntitiesToList())
        {
            var crop = new FarmCropRowData
            {
                cropId     = e.Get<int>("Crops_ID"),
                cropName   = e.Get<string>("Crops_Name"),
                seedItemId = e.Get<int>("Seed_ID"),
            };

            // ===== Growth Stages =====
            // 테이블 구조상 Seed(1) / Seedling(2) / Mid(3) / Fruitling(프리팹만 존재)
            var stages = new List<FarmGrowthStageData>(4);

            // Stage 1 (Seed)
            stages.Add(new FarmGrowthStageData
            {
                prefabKey = e.Get<string>("Prefab_Seed"),
                needTime  = e.Get<int>("NeedTime_1"),
                needWater = e.Get<int>("NeedWater_1"),
            });

            // Stage 2 (Seedling)
            stages.Add(new FarmGrowthStageData
            {
                prefabKey = e.Get<string>("Prefab_Seedling"),
                needTime  = e.Get<int>("NeedTime_2"),
                needWater = e.Get<int>("NeedWater_2"),
            });

            // Stage 3 (MidGrowth)
            stages.Add(new FarmGrowthStageData
            {
                prefabKey = e.Get<string>("Prefab_MidGrowth"),
                needTime  = e.Get<int>("NeedTime_3"),
                needWater = e.Get<int>("NeedWater_3"),
            });

            // Stage 4 (Fruitling / Final Visual)
            // 테이블에 시간/물 컬럼이 없으니 0으로 넣고 “최종 프리팹”만 보관
            stages.Add(new FarmGrowthStageData
            {
                prefabKey = e.Get<string>("Prefab_Fruitling"),
                needTime  = 0,
                needWater = 0,
            });

            crop.stages = stages.ToArray();

            // ===== Drops =====
            var drops = new List<FarmHarvestDropData>(3);

            // Item1 (보장)
            {
                int id = e.Get<int>("Item1_ID");
                int cnt = e.Get<int>("Item_1Count");
                if (id != 0 && cnt > 0)
                {
                    drops.Add(new FarmHarvestDropData
                    {
                        itemId = id,
                        count  = cnt,
                        rate   = 1f
                    });
                }
            }

            // Item2 (보장)
            {
                int id = e.Get<int>("Item2_ID");
                int cnt = e.Get<int>("Item2_Count");
                if (id != 0 && cnt > 0)
                {
                    drops.Add(new FarmHarvestDropData
                    {
                        itemId = id,
                        count  = cnt,
                        rate   = 1f
                    });
                }
            }

            // Item3 (확률)
            {
                int id = e.Get<int>("Item3_ID");
                float rate = e.Get<float>("Item3_rate");
                int cnt = e.Get<int>("Item3_Count");

                if (id != 0 && cnt > 0 && rate > 0f)
                {
                    drops.Add(new FarmHarvestDropData
                    {
                        itemId = id,
                        count  = cnt,
                        rate   = rate
                    });
                }
            }

            crop.drops = drops.ToArray();

            _crops[crop.cropId] = crop;
        }
    }

    public bool TryGet(int cropId, out FarmCropRowData data)
        => _crops.TryGetValue(cropId, out data);

    public Dictionary<int, FarmCropRowData> GetAll()
        => _crops;

    public bool TryGetBySeedId(int seedItemId, out FarmCropRowData row)
    {
        foreach (var kvp in _crops)
        {
            if (kvp.Value.seedItemId == seedItemId)
            {
                row = kvp.Value;
                return true;
            }
        }
        row = null;
        return false;
    }

    public bool IsSeed(int itemId)
    {
        foreach (var kvp in _crops)
        {
            if (kvp.Value.seedItemId == itemId)
            {
                return true;
            }
        }
        return false;
    }

    public List<FarmCropRowData> GetBySeedId(int seedItemId)
    {
        var list = new List<FarmCropRowData>();
        foreach (var kvp in _crops)
        {
            if (kvp.Value.seedItemId == seedItemId)
                list.Add(kvp.Value);
        }
        return list;
    }
}

