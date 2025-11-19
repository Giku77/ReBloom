using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class GatherObjectDB
{
    private Dictionary<int, GatherObjectData> dataById = new Dictionary<int, GatherObjectData>();

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
            Debug.Log($"[GatherObjectDB] ID {data.id} 로드됨");
        }

        Debug.Log($"[GatherObjectDB] 총 {dataById.Count}개 로드 완료");
    }

    private GatherObjectData ParseDebuff(BGEntity entity)
    {
        var data = new GatherObjectData();

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

    public bool TryGet(int id, out GatherObjectData data)
    {
        return dataById.TryGetValue(id, out data);
    }

    public Dictionary<int, GatherObjectData> GetAll()
    {
        return dataById;
    }
}
