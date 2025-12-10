using UnityEngine;

[CreateAssetMenu(menuName = "ReBloom/Farming/CropData")]
public class CropData : ScriptableObject
{
    public int cropId;                 // 나중에 DB용 ID
    public string cropName;
    public int seedItemId;         // 씨앗 아이템
    public int harvestItemId;      // 수확 결과 아이템

    [System.Serializable]
    public class GrowthStage
    {
        public float duration;        // 이 단계에서 다음 단계까지 걸리는 시간(초/게임시간 틱)
        public GameObject prefab;     // 이 단계에서 보여줄 프리팹
    }

    public GrowthStage[] stages;

    public bool NeedsWaterEachStage = true;  // 물 요구 여부 등 옵션
}
