using System;
using UnityEngine;

public enum CultivationSlotState
{
    Empty,
    Running,
    ReadyToCollect
}

[Serializable]
public class CultivationSlot
{
    public CultivationSlotState state = CultivationSlotState.Empty;

    public int cropId;
    public int seedItemId;
    public float remainTime;           

    public int outputItemId;
    public int outputCount;

    public float requiredPowerKw;
}
