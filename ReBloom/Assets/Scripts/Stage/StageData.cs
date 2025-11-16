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
    public float rainRate;
    public float rain_d;
    public float rain_vari;
    public float rainPollution;
    public float rainThirst;
    public float rainTemp;
    public float radioRate;
    public float radio_d;
    public float radio_vari;
    public float radioPollution;
    public float radioThirst;
    public float radioTemp;
    public float snowRate;
    public float snow_d;
    public float snow_vari;
    public float snowPollution;
    public float snowThirst;
    public float snowTemp;
    public float thunderRate;
    public float thunde_d;
    public float thunde_vari;
    public float thundePollution;
    public float thundeThirst;
    public float thundeTemp;
    public float hotRate;
    public float hot_d;
    public float hot_vari;
    public float hotPollution;
    public float hotThirst;
    public float hotTemp;


    public StageData() {}

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
