using BansheeGz.BGDatabase;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GatherDB
{
    private Dictionary<int, GatherData> dataById = new Dictionary<int, GatherData>();

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
            var data = ParseGather(entity);
            dataById[data.id] = data;
        }
    }

    private GatherData ParseGather(BGEntity entity)
    {
        var data = new GatherData();

        data.id = entity.Get<int>("GatherID");
        data.getItem1 = entity.Get<int>("GetItem1");
        data.item1Probability = entity.Get<float>("Item1Probability");
        data.item1MinAmount = entity.Get<int>("Item1MinAmount");
        data.item1MaxAmount = entity.Get<int>("Item1MaxAmount");
        data.getItem2 = entity.Get<int>("GetItem2");
        data.item2Probability = entity.Get<float>("Item2Probability");
        data.item2MinAmount = entity.Get<int>("Item2MinAmount");
        data.item2MaxAmount = entity.Get<int>("Item2MaxAmount");
        data.nightMultiple = entity.Get<int>("NightMultiple");

        return data;
    }

    public bool TryGet(int id, out GatherData data) => dataById.TryGetValue(id, out data);


    public Dictionary<int, GatherData> GetAll()
    {
        return dataById;
    }
}
