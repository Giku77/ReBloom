using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class GatheringObjectDB
{
    private Dictionary<int, GatheringObjectData> dataById = new Dictionary<int, GatheringObjectData>();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Gathering_Object");
        if (meta == null)
        {
            Debug.LogWarning("[GatheringObjectDB] 'GatheringObject' 데이터 테이블을 찾을 수 없습니다.");
            return;
        }

        dataById.Clear();

        foreach (var entity in meta.EntitiesToList())
        {
            var data = ParseDebuff(entity);
            dataById[data.id] = data;
        }
    }

    private GatheringObjectData ParseDebuff(BGEntity entity)
    {
        var data = new GatheringObjectData();

        data.id = entity.Get<int>("ObjectID");
        data.objectNameId = entity.Get<string>("ObjectNameID");
        data.searchTime = entity.Get<float>("SearchTime");
        data.respawnTime = entity.Get<float>("RespawnTime");
        data.gatherId = entity.Get<int>("GatherID");
        data.handSearchType = entity.Get<int>("HandSearchType");
        data.shovelSearchType = entity.Get<int>("ShovelSearchType");
        data.handSearchType = entity.Get<int>("HammerSearchType");

        return data;
    }

    public bool TryGet(int id, out GatheringObjectData data)
    {
        return dataById.TryGetValue(id, out data);
    }

    public Dictionary<int, GatheringObjectData> GetAll()
    {
        return dataById;
    }
}
