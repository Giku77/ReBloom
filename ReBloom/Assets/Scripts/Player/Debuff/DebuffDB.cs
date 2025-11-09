using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BG Database에서 디버프 데이터를 로드하는 클래스
/// QuestDB, ItemDatabase와 동일한 패턴
/// </summary>
public class DebuffDB
{
    private Dictionary<int, DebuffData> dataById = new Dictionary<int, DebuffData>();
    
    /// <summary>
    /// BG Database에서 Debuff 테이블 로드
    /// </summary>
/// <summary>
    /// 하드코딩된 디버프 데이터 로드 (테스트용)
    /// </summary>
    public void LoadFromBG()
    {
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
        
        Debug.Log($"[DebuffDB] 하드코딩된 {dataById.Count}개의 디버프 데이터 로드 완료");
    }
    
    /// <summary>
    /// BGEntity를 DebuffData로 파싱
    /// </summary>

    
    /// <summary>
    /// ID로 데이터 조회
    /// </summary>
    public bool TryGet(int id, out DebuffData data)
    {
        return dataById.TryGetValue(id, out data);
    }
    
    /// <summary>
    /// 모든 데이터 반환
    /// </summary>
    public Dictionary<int, DebuffData> GetAll()
    {
        return dataById;
    }
    
    /// <summary>
    /// 카테고리별 데이터 조회 (1=중독, 2=갈증, 3=허기)
    /// </summary>
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