using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class GatheringDB
{
    private Dictionary<int, GatheringData> dataById = new Dictionary<int, GatheringData>();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Gathering");
        if (meta == null)
        {
            Debug.LogWarning("[GatheringDB] 'Gathering' 데이터 테이블을 찾을 수 없습니다.");
            return;
        }

        dataById.Clear();

        foreach (var entity in meta.EntitiesToList())
        {
            var data = ParseDebuff(entity);
            dataById[data.id] = data;
        }
    }

    private GatheringData ParseDebuff(BGEntity entity)
    {
        var data = new GatheringData();

        data.id = entity.Get<int>("GatherID");
        data.GetItem1 = entity.Get<int>("GetItem1");
        data.item1Probability = entity.Get<float>("item1Probability");
        data.item1MinAmount = entity.Get<int>("item1MinAmount");
        data.item1MaxAmount = entity.Get<int>("item1MaxAmount");
        data.GetItem2 = entity.Get<int>("GetItem2");
        data.item2Probability = entity.Get<float>("item2Probability");
        data.item2MinAmount = entity.Get<int>("item2MinAmount");
        data.item2MaxAmount = entity.Get<int>("item2MaxAmount");

        return data;
    }

    public bool TryGet(int id, out GatheringData data)
    {
        return dataById.TryGetValue(id, out data);
    }

    public Dictionary<int, GatheringData> GetAll()
    {
        return dataById;
    }
}
