using UnityEngine;

[CreateAssetMenu(fileName = "IconProfile", menuName = "Tools/Prefab Icon Profile")]
public class PrefabIconProfile : ScriptableObject
{
    [Header("Target")]
    public GameObject prefab;

    [Header("Camera Settings")]
    public Vector3 cameraRotation = new Vector3(30f, 45f, 0f);
    public float fov = 30f;
    public float cameraDistance = 1f;

    [Header("Prefab Transform")]
    public Vector3 prefabRotation = Vector3.zero;
    public Vector3 prefabOffset = Vector3.zero;
    public float prefabScale = 1f;

    [Header("Main Light")]
    public Vector3 mainLightRotation = new Vector3(50f, 30f, 0f);
    public float mainLightIntensity = 1.2f;
    public Color mainLightColor = Color.white;

    [Header("Fill Light")]
    public bool enableFillLight = true;
    public float fillLightIntensity = 0.4f;
    public Color fillLightColor = new Color(0.9f, 0.9f, 1f);

    [Header("Rim Light")]
    public bool enableRimLight = false;
    public float rimLightIntensity = 0.5f;
    public Color rimLightColor = Color.white;

    [Header("Ambient")]
    public float ambientIntensity = 0.5f;

    public void InitializeDefaults()
    {
        cameraRotation = new Vector3(30f, 45f, 0f);
        fov = 30f;
        cameraDistance = 1f;

        prefabRotation = Vector3.zero;
        prefabOffset = Vector3.zero;
        prefabScale = 1f;

        mainLightRotation = new Vector3(50f, 30f, 0f);
        mainLightIntensity = 1.2f;
        mainLightColor = Color.white;

        enableFillLight = true;
        fillLightIntensity = 0.4f;
        fillLightColor = new Color(0.9f, 0.9f, 1f);

        enableRimLight = false;
        rimLightIntensity = 0.5f;
        rimLightColor = Color.white;

        ambientIntensity = 0.5f;
    }
}