using UnityEngine;

public enum CropSlotState
{
    Empty,
    Growing,
    Mature,
    Withered
}

[System.Serializable]
public class CropSlot
{
    public CropSlotState state = CropSlotState.Empty;

    public int cropId;         
    public int stageIndex;
    public float stageTimer;

    public int wateredCount;    
    // 시각화용
    public GameObject visualRoot;
    [HideInInspector] public CropVisual visual;
}
