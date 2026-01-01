using System;
using UnityEngine;

[Serializable]
public class GreeningRow
{
    public int greeningId;

    // 이 단계가 적용되는 최소 greening 값 (예: 20, 25, 30...)
    public float minGreening;

    public string planetKey;   // "PGreening20per" 또는 "0"
    public string animalKey;   // "AGreening80per" 또는 "0"
    public string insectKey;   // "IGreening60per" 또는 "0"

    public Color terrainColor; // 0~1 컬러
    public Color fogColor;
}
