using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class StageData
{
    public int id;
    public string name;
    public int stageVariation;
    public float stagePollutuion;
    public int stageTemp;
    public float sunnyRate;
    public int sunny_d;
    public int sunny_vari;
    public float sunnyPollution;
    public float sunnyThirst;
    public int sunnyTemp;

    public StageData() { }

    public StageData(int id, string name, int sv, float sp, int st, float sr, int sd, int svv, float spp, float stt, int sttt)
    { 
        this.id = id;
        this.name = name;
        stageVariation = sv;
        stagePollutuion = sp;
        stageTemp = st;
        sunnyRate = sr;
        sunny_d = sd;
        sunny_vari = svv;
        sunnyPollution = spp;
        sunnyThirst = stt;
        sunnyTemp = sttt;
    }
}
