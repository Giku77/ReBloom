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
            Debug.LogWarning("[DebuffDB] 'Stage' 데이터 테이블을 찾을 수 없습니다.");
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
        data.stageVariation = entity.Get<float>("Stage_Variation");
        data.stagePollution = entity.Get<float>("Stage_Pollution");
        data.stageTemp = entity.Get<float>("Stage_Temp");
        
        data.sunnyRate = entity.Get<float>("Sunny_Rate");
        data.sunny_d = entity.Get<float>("Sunny_D");
        data.sunny_vari = entity.Get<float>("Sunny_Vari");
        data.sunnyPollution = entity.Get<float>("Sunny_Pollution");
        data.sunnyThirst = entity.Get<float>("Sunny_Thirst");
        data.sunnyTemp = entity.Get<float>("Sunny_Temp");
        
        data.rainRate = entity.Get<float>("Rain_Rate");
        data.rain_d = entity.Get<float>("Rain_D");
        data.rain_vari = entity.Get<float>("Rain_Vari");
        data.rainPollution = entity.Get<float>("Rain_Pollution");
        data.rainThirst = entity.Get<float>("Rain_Thirst");
        data.rainTemp = entity.Get<float>("Rain_Temp");
        
        data.radioRate = entity.Get<float>("Radio_Rate");
        data.radio_d = entity.Get<float>("Radio_D");
        data.radio_vari = entity.Get<float>("Radio_Vari");
        data.radioPollution = entity.Get<float>("Radio_Pollution");
        data.radioThirst = entity.Get<float>("Radio_Thirst");
        data.radioTemp = entity.Get<float>("Radio_Temp");
        
        data.snowRate = entity.Get<float>("Snow_Rate");
        data.snow_d = entity.Get<float>("Snow_D");
        data.snow_vari = entity.Get<float>("Snow_Vari");
        data.snowPollution = entity.Get<float>("Snow_Pollution");
        data.snowThirst = entity.Get<float>("Snow_Thirst");
        data.snowTemp = entity.Get<float>("Snow_Temp");

        data.thunderRate = entity.Get<float>("Thunder_Rate");
        data.thunde_d = entity.Get<float>("Thunder_D");
        data.thunde_vari = entity.Get<float>("Thunder_Vari");
        data.thundePollution = entity.Get<float>("Thunder_Poll");
        data.thundeThirst = entity.Get<float>("Thunder_Thirst");
        data.thundeTemp = entity.Get<float>("Thunder_Temp");
        
        data.hotRate = entity.Get<float>("Hot_Rate");
        data.hot_d = entity.Get<float>("Hot_D");
        data.hot_vari = entity.Get<float>("Hot_Vari");
        data.hotPollution = entity.Get<float>("Hot_Pollution");
        data.hotThirst = entity.Get<float>("Hot_Thirst");
        data.hotTemp = entity.Get<float>("Hot_Temp");

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
