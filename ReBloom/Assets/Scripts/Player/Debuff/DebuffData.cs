using UnityEngine;

public class DebuffData
{
    public int id;
    public string name;
    public int debuffCat;
    public bool isMultiAble;
    public float standardValue;
    public float hpLoss;
    public float speedReduce;
    
    public float duration = -1f;
    
    public DebuffData() { }
    
    public DebuffData(int id, string name, int category, bool isStackable, 
                     float triggerThreshold, float hpLoss, float speedReduction)
    {
        this.id = id;
        this.name = name;
        this.debuffCat = category;
        this.isMultiAble = isStackable;
        this.standardValue = triggerThreshold;
        this.hpLoss = hpLoss;
        this.speedReduce = speedReduction;
    }
}