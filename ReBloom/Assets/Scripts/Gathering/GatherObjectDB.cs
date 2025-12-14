using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class GatherObjectDB
{
    private Dictionary<int, GatherObjectData> dataById = new Dictionary<int, GatherObjectData>();
    private Dictionary<int, ObjectStringData> stringData = new Dictionary<int, ObjectStringData>();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Gathering_Object");
        var meatString = BGRepo.I.GetMeta("Object_String");

        if (meta == null)
        {
            Debug.LogWarning("[GatheringObjectDB] 'GatheringObject' 데이터 테이블을 찾을 수 없습니다.");
            return;
        }
        if (meatString == null)
        {
            Debug.LogWarning("[GatheringObjectDB] 'ObjectString' 데이터 테이블을 찾을 수 없습니다.");
            return;
        }

        dataById.Clear();
        stringData.Clear();

        foreach (var entity in meta.EntitiesToList())
        {
            var data = ParseDebuff(entity);
            dataById[data.id] = data;
            Debug.Log($"[GatherObjectDB] ID {data.id} 로드됨");
        }
        foreach (var entity in meatString.EntitiesToList())
        {
            var data = new ObjectStringData
            {
                stringID = entity.Get<int>("stringID"),
                stringKOR = entity.Get<string>("stringKOR"),
                stringENG = entity.Get<string>("stringENG")
            };

            stringData[data.stringID] = data;
        }

        Debug.Log($"[GatherObjectDB] 총 {dataById.Count}개 로드 완료");
    }

    private GatherObjectData ParseDebuff(BGEntity entity)
    {
        var data = new GatherObjectData();

        data.id = entity.Get<int>("ObjectID");
        data.objectNameId = entity.Get<int>("ObjectNameID");
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
    public bool TryGetString(int stringId, out ObjectStringData data) => stringData.TryGetValue(stringId, out data);
    public string GetTextKR(int stringId) => stringData.TryGetValue(stringId, out var d) ? d.stringKOR : $"#{stringId}";

    public Dictionary<int, GatherObjectData> GetAll()
    {
        return dataById;
    }
}
