using UnityEngine;

public class StageBase : MonoBehaviour
{
    [Header("Stage Data")]
    [SerializeField] private int stageID;
    
    private StageData stageData;
    
    public int StageID => stageID;
    public StageData Data => stageData;
    
    public void Initialize(StageDB db)
    {
        if (db.TryGet(stageID, out stageData))
        {
            Debug.Log($"[Stage] 지역 초기화 성공: ID={stageID}, Name={stageData.name}, Pollution={stageData.stagePollution}");
        }
        else
        {
            Debug.LogError($"[Stage] StageDB에서 ID={stageID}를 찾을 수 없습니다!");
        }
    }
}
