using UnityEngine;

public class ArcData
{
    public int arcId;              
    public string name;             
    public int tier;                
    public int arcType;            
    public float energyInc;
    public float energyDec;
    public float researchInc;
    public float greeningInc;
    public int unlockValue;
    public int installLimit;
    public float buildTime;        
    public string text;
    public int arcInteraction;
    public float interactTime;
    public string interactText;
    public int arrangePossible;
    public GameObject buildPrefab;   
    public GameObject previewPrefab;
}

public class ArcContext
{
    public ArcData Data;       
    public GameObject ArcPrefab;
    public Vector3 Position;        
    public Quaternion Rotation;
    public BuildingFootprint FootPrint;
    public Transform PlayerTransform;

    public float DepthOffset;
    public BuildingInstance IgnoreOccupancyInstance;
}
