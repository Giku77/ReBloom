using UnityEngine;

[System.Serializable]
public struct BuildingFootprint
{
    public float sizeX; // 가로
    public float sizeZ; // 세로
    public float height; // 높이

}

public class BuildingFootprintProvider : MonoBehaviour
{
    [SerializeField] private BoxCollider buildArea; // BuildArea에 있는 콜라이더

    public BuildingFootprint GetFootprint(ArcData data)
    {
        if (data.buildPrefab == null)
        {
            Debug.LogWarning($"ArcData {data.arcId} 에 buildPrefab이 없습니다. 기본 BuildArea 사이즈 사용.");
            return FromCollider(buildArea);
        }

        if (data.buildPrefab.TryGetComponent<BoxCollider>(out var box))
        {
            return FromCollider(box);
        }

        Debug.LogWarning($"Prefab {data.buildPrefab.name} 에 BoxCollider가 없습니다. 기본 BuildArea 사이즈 사용.");
        return FromCollider(buildArea);
    }

    private BuildingFootprint FromCollider(BoxCollider col)
    {
        var size = col.size;
        var scale = col.transform.lossyScale;

        return new BuildingFootprint
        {
            sizeX = size.x * scale.x,
            sizeZ = size.z * scale.z,
            height = size.y * scale.y
        };
    }
}
