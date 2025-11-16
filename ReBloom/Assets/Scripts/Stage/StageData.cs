using UnityEngine;

public class StageData
{
    public int id;
    public string name;
    public float stageVariation;
    public float stagePollution;
    public float stageTemp;
    public float sunnyRate;
    public float sunny_d;
    public float sunny_vari;
    public float sunnyPollution;
    public float sunnyThirst;
    public float sunnyTemp;

    public StageData() { }

    public StageData(int id, string name, int sv, float sp, int st, float sr, int sd, int svv, float spp, float stt, int sttt)
    { 
        this.id = id;
        this.name = name;
        stageVariation = sv;
        stagePollution = sp;
        stageTemp = st;
        sunnyRate = sr;
        sunny_d = sd;
        sunny_vari = svv;
        sunnyPollution = spp;
        sunnyThirst = stt;
        sunnyTemp = sttt;
    }
}
