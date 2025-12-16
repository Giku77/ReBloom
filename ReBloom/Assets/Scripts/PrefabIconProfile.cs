using UnityEngine;

public class PrefabIconProfile : ScriptableObject
{
    public GameObject prefab;

    [Header("Transform")]
    public Vector3 rotation;
    public float cameraDistance = 3f;

    [Header("Camera")]
    public float fov = 30f;

    [Header("Output")]
    public int outputResolution = 512;
}