using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class DebuffDB
{
    private Dictionary<int, DebuffData> dataById = new Dictionary<int, DebuffData>();
    
    public void LoadFromBG()
    {
        //var meta = BGRepo.I.GetMeta("Debuff");
        //if (meta == null)
        //{
        //    Debug.LogWarning("[DebuffDB] 'Debuff' 테이블을 찾을 수 없습니다.");
        //    return;
        //}

        //dataById.Clear();

        //foreach (var entity in meta.EntitiesToList())
        //{
        //    var data = ParseDebuff(entity);
        //    dataById[data.id] = data;
        //}

        dataById.Clear();
        
        // 중독 (오염도 100)
        dataById[210] = new DebuffData(210, "중독상태", 1, true, 100f, 5f, 0f);
        
        // 갈증 단계
        dataById[220] = new DebuffData(220, "갈증 1단계", 2, true, 30f, 0f, 0.1f);
        dataById[221] = new DebuffData(221, "갈증 2단계", 2, true, 50f, 0f, 0.3f);
        dataById[222] = new DebuffData(222, "탈수", 2, true, 100f, 5f, 0.3f);
        
        // 허기 단계
        dataById[230] = new DebuffData(230, "허기 1단계", 3, true, 30f, 0f, 0.1f);
        dataById[231] = new DebuffData(231, "허기 2단계", 3, true, 50f, 0f, 0.3f);
        dataById[232] = new DebuffData(232, "기아", 3, true, 100f, 5f, 0.3f);
    }

    //private DebuffData ParseDebuff(BGEntity entity)
    //{
    //    var data = new DebuffData();

    //    data.id = entity.Get<int>("Debuff_ID");
    //    data.name = entity.Get<string>("Debuff_Name");
    //    data.debuffCat = entity.Get<int>("Debuff_Cat");
    //    data.isMultiAble = entity.Get<int>("Multi_able") == 1;
    //    data.standardValue = entity.Get<float>("Standard_Value");
    //    data.hpLoss = entity.Get<float>("Hp_Loss");
    //    data.speedReduce = entity.Get<float>("Speed_Reduce");

    //    return data;
    //}

    public bool TryGet(int id, out DebuffData data)
    {
        return dataById.TryGetValue(id, out data);
    }
    
    public Dictionary<int, DebuffData> GetAll()
    {
        return dataById;
    }
    
    public List<DebuffData> GetByCategory(int category)
    {
        List<DebuffData> result = new List<DebuffData>();
        
        foreach (var data in dataById.Values)
        {
            if (data.debuffCat == category)
            {
                result.Add(data);
            }
        }
        
        return result;
    }
}