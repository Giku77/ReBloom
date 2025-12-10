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
    public CropData crop;
    public int stageIndex;
    public float stageTimer;
    public bool watered;

    // 시각화용
    [HideInInspector] public CropVisual visual;
}
