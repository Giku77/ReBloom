using System;
using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public int arcId; 

    public float depthOffset = 0f;

    public int ArcId => arcId;
    public ArcData Data { get; private set; }

    private void OnEnable()
    {
        if (ResearchManager.I != null)
        {
            ResearchManager.I.RegisterBuilding(this);
        }
        if (BuildManager.I != null)
        {
            Debug.Log($"[BuildingInstance] Registering building instance with ArcId={arcId}");
            BuildManager.I.RegisterBuilding(this);
        }
    }

    private void OnDisable()
    {
        if (ResearchManager.I != null)
        {
            ResearchManager.I.UnregisterBuilding(this);
        }
        if (BuildManager.I != null)
        {
            BuildManager.I.UnregisterBuilding(this);
        }
    }

    private void Awake()
    {
        BuildManager.I.ArcDB.TryGet(arcId, out var arc);
        Data = arc;
    }
}
