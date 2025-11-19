using System;
using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public int arcId; 

    public int ArcId => arcId;
    public ArcData Data { get; private set; }

    private void Awake()
    {
        BuildManager.I.ArcDB.TryGet(arcId, out var arc);
        Data = arc;
    }
}
