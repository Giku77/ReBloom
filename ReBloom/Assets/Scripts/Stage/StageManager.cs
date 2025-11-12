using UnityEngine;

public class StageManager : MonoBehaviour
{ 
    private StageDB stageDB;
    
    public StageDB DB => stageDB;
    
    private void Awake()
    {    
        stageDB = new StageDB();
        stageDB.LoadFromBG();
        
        Debug.Log($"[StageManager] StageDB 로드 완료. 총 {stageDB.GetAll().Count}개의 Stage 데이터");
    }
    
    private void Start()
    {
        InitializeAllStages();
    }
    
    private void InitializeAllStages()
    {
        StageBase[] stages = FindObjectsOfType<StageBase>();
        
        foreach (var stage in stages)
        {
            stage.Initialize(stageDB);
        }
        
        Debug.Log($"[StageManager] {stages.Length}개의 Stage 오브젝트 초기화 완료");
    }

    public bool TryGetStageData(int id, out StageData data)
    {
        return stageDB.TryGet(id, out data);
    }
}
