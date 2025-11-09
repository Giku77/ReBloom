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
    
    public DebuffData(int id, string name, int debuffCat, bool isMultiAble, float standardValue, float hpLoss, float speedReduce)
    {
        this.id = id;
        this.name = name;
        this.debuffCat = debuffCat;
        this.isMultiAble = isMultiAble;
        this.standardValue = standardValue;
        this.hpLoss = hpLoss;
        this.speedReduce = speedReduce;
    }
}