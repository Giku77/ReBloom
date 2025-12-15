[System.Serializable]
public class FarmCellState
{
    public bool HasCrop;        // 빈칸인지?
    public int CropId;          // 어떤 작물인지 (Crops_ID)
    public int StageIndex;      // 0=씨앗, 1=새싹, 2=중간, 3=열매 ...
    
    public float StageElapsed;  // 현재 스테이지에서 지난 시간(초 or 게임시간)
    public int WateredCount;    // 현재 스테이지에서 물 준 횟수

    public bool CanHarvest;     // 수확 가능 상태인지 (StageIndex가 마지막이고 시간 완료 등)
    public bool Fertilized;     // 비료 적용 여부(옵션)
    
    // UI용 편의 값(계산해서 보여주기용)
    public float RemainSeconds;
    public int NeedWaterCount;
}
