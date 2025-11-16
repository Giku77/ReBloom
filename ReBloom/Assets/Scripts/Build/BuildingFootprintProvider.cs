using UnityEngine;

[System.Serializable]
public struct BuildingFootprint
{
    public float sizeX; // 가로
    public float sizeZ; // 세로
}

public class BuildingFootprintProvider : MonoBehaviour
{
    [SerializeField] private BoxCollider buildArea; // BuildArea에 있는 콜라이더

    public BuildingFootprint GetFootprint()
    {
        // scale이 1,1,1이라고 가정하면 그냥 size.x / size.z 쓰면 됨
        var size = buildArea.size;
        return new BuildingFootprint
        {
            sizeX = size.x,
            sizeZ = size.z
        };
    }
}
