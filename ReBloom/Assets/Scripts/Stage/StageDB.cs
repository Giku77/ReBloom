using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class StageDB
{
    private Dictionary<int, StageData> dataById = new Dictionary<int, StageData>();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Stage");
        if (meta == null)
        {
            Debug.LogWarning("[DebuffDB] 'Stage' 테이블을 찾을 수 없습니다.");
            return;
        }

        dataById.Clear();

        foreach (var entity in meta.EntitiesToList())
        {
            var data = ParseDebuff(entity);
            dataById[data.id] = data;
        }
    }

    private StageData ParseDebuff(BGEntity entity)
    {
        var data = new StageData();

        data.id = entity.Get<int>("Stage_ID");
        data.name = entity.Get<string>("Stage_Name");
        data.stageVariation = entity.Get<int>("Stage_Variation");
        data.stagePollutuion = entity.Get<int>("Stage_Pollution");
        data.stageTemp = entity.Get<int>("Stage_Temp");
        data.sunnyRate = entity.Get<float>("Sunny_Rate");
        data.sunny_d = entity.Get<int>("Sunny_D");
        data.sunny_vari = entity.Get<int>("Sunny_Vari");
        data.sunnyPollution = entity.Get<int>("Sunny_Pollution");
        data.sunnyThirst = entity.Get<int>("Sunny_Thirst");
        data.sunnyTemp = entity.Get<int>("Sunny_Temp");

        return data;
    }

    public bool TryGet(int id, out StageData data)
    {
        return dataById.TryGetValue(id, out data);
    }

    public Dictionary<int, StageData> GetAll()
    {
        return dataById;
    }
}
