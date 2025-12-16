using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabIconGeneratorAdvanced : EditorWindow
{
    [MenuItem("Tools/Advanced Prefab Icon Generator")]
    static void Open()
        => GetWindow<PrefabIconGeneratorAdvanced>("Prefab Icon Generator");

    private List<PrefabIconProfile> profiles = new();
    private PrefabIconProfile currentProfile;

    private GameObject previewInstance;
    private Camera previewCamera;
    private Light previewLight;
    private RenderTexture previewRT;

    private const int PREVIEW_SIZE = 256;
    private Vector2 scroll;

    // ==============================
    // LIFECYCLE
    // ==============================
    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        Cleanup();
    }

    // ==============================
    // GUI
    // ==============================
    private void OnGUI()
    {
        HandleDragAndDrop();

        DrawToolbar();
        DrawProfileList();

        if (currentProfile != null)
        {
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawPreview();
            EditorGUILayout.Space();
            DrawActionButtons();
        }
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Batch Export", EditorStyles.toolbarButton))
            BatchExport();

        GUILayout.FlexibleSpace();
        GUILayout.Label("Drag Prefabs Here", EditorStyles.miniLabel);

        GUILayout.EndHorizontal();
    }

    // ==============================
    // PROFILE LIST
    // ==============================
    private void DrawProfileList()
    {
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(140));

        foreach (var p in new List<PrefabIconProfile>(profiles))
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(p == currentProfile, p.prefab.name, "Button"))
                SelectProfile(p);

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                RemoveProfile(p);
                return;
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void RemoveProfile(PrefabIconProfile profile)
    {
        if (currentProfile == profile)
        {
            currentProfile = null;
            Cleanup();
        }

        profiles.Remove(profile);
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(profile));
        AssetDatabase.SaveAssets();
    }

    // ==============================
    // SETTINGS
    // ==============================
    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);

        currentProfile.rotation =
            EditorGUILayout.Vector3Field("Rotation", currentProfile.rotation);

        currentProfile.fov =
            EditorGUILayout.Slider("FOV", currentProfile.fov, 15f, 60f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        currentProfile.outputResolution =
            EditorGUILayout.IntPopup(
                "Resolution",
                currentProfile.outputResolution,
                new[] { "256", "512", "1024" },
                new[] { 256, 512, 1024 }
            );

        EditorUtility.SetDirty(currentProfile);
    }

    // ==============================
    // PREVIEW
    // ==============================
    private void DrawPreview()
    {
        GUILayout.Label("Preview (Drag to Rotate)", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE, GUILayout.ExpandWidth(false));

        if (previewRT)
            EditorGUI.DrawPreviewTexture(rect, previewRT);

        HandlePreviewRotation(rect);
    }

    private void HandlePreviewRotation(Rect rect)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition))
            return;

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            Quaternion q = Quaternion.Euler(currentProfile.rotation);

            Quaternion yaw = Quaternion.AngleAxis(e.delta.x, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(-e.delta.y, Vector3.right);

            q = yaw * pitch * q;
            currentProfile.rotation = q.eulerAngles;

            e.Use();
            Repaint();
        }
    }

    private void DrawActionButtons()
    {
        if (GUILayout.Button("Save Screenshot"))
            SaveScreenshot();
    }

    // ==============================
    // DRAG & DROP
    // ==============================
    private void HandleDragAndDrop()
    {
        Event e = Event.current;

        if (e.type != EventType.DragUpdated &&
            e.type != EventType.DragPerform)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (Object o in DragAndDrop.objectReferences)
            {
                if (o is GameObject go &&
                    PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    CreateProfile(go);
                }
            }
        }

        e.Use();
    }

    // ==============================
    // PROFILE
    // ==============================
    private void CreateProfile(GameObject prefab)
    {
        if (profiles.Exists(p => p.prefab == prefab))
            return;

        var profile = ScriptableObject.CreateInstance<PrefabIconProfile>();
        profile.prefab = prefab;

        AssetDatabase.CreateAsset(
            profile,
            $"Assets/{prefab.name}_IconProfile.asset");

        AssetDatabase.SaveAssets();

        profiles.Add(profile);
        SelectProfile(profile);
    }

    private void SelectProfile(PrefabIconProfile profile)
    {
        if (currentProfile == profile)
            return;

        currentProfile = profile;
        RebuildPreview();
    }

    // ==============================
    // PREVIEW CORE
    // ==============================
    private void RebuildPreview()
    {
        Cleanup();

        previewInstance =
            (GameObject)PrefabUtility.InstantiatePrefab(currentProfile.prefab);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        NormalizePivot(previewInstance);

        previewCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
        previewCamera.hideFlags = HideFlags.HideAndDontSave;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;

        previewLight = new GameObject("PreviewLight").AddComponent<Light>();
        previewLight.hideFlags = HideFlags.HideAndDontSave;
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.transform.rotation = Quaternion.Euler(50, 30, 0);

        previewRT = new RenderTexture(PREVIEW_SIZE, PREVIEW_SIZE, 24, RenderTextureFormat.ARGB32);
        previewCamera.targetTexture = previewRT;
    }

    private void UpdatePreview()
    {
        RenderPreviewOnce();
    }

    private void RenderPreviewOnce()
    {
        if (!previewInstance || !previewCamera || !currentProfile)
            return;

        previewInstance.transform.rotation =
            Quaternion.Euler(currentProfile.rotation);

        Bounds b = CalculateBounds(previewInstance);
        float dist = CalculateAutoCameraDistance(b, currentProfile.fov) * 1.1f;

        Vector3 center = b.center;

        previewCamera.transform.position =
            center + previewInstance.transform.rotation * Vector3.back * dist;

        previewCamera.transform.LookAt(center);
        previewCamera.fieldOfView = currentProfile.fov;

        previewCamera.Render();
    }

    private void Cleanup()
    {
        if (previewInstance) DestroyImmediate(previewInstance);
        if (previewCamera) DestroyImmediate(previewCamera.gameObject);
        if (previewLight) DestroyImmediate(previewLight.gameObject);
        if (previewRT) previewRT.Release();
    }

    // ==============================
    // EXPORT
    // ==============================
    private void SaveScreenshot()
    {
        string folder = EditorUtility.OpenFolderPanel("Save Icon", "Assets", "");
        if (string.IsNullOrEmpty(folder))
            return;

        RenderPreviewOnce();
        RenderPreviewOnce();

        SaveScreenshotInternal(folder, currentProfile);
        AssetDatabase.Refresh();
    }

    private void BatchExport()
    {
        string folder = EditorUtility.OpenFolderPanel("Batch Export Icons", "Assets", "");
        if (string.IsNullOrEmpty(folder))
            return;

        foreach (var profile in profiles)
        {
            currentProfile = profile;
            RebuildPreview();

            RenderPreviewOnce();
            RenderPreviewOnce();

            SaveScreenshotInternal(folder, profile);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Batch Export", "All icons exported.", "OK");
    }

    private void SaveScreenshotInternal(string folder, PrefabIconProfile profile)
    {
        int res = profile.outputResolution;

        RenderTexture rt = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32);
        previewCamera.targetTexture = rt;

        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        previewCamera.Render();

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        previewCamera.targetTexture = previewRT;
        RenderTexture.active = null;
        rt.Release();

        string path = System.IO.Path.Combine(folder, profile.prefab.name + "_icon.png");
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

        DestroyImmediate(tex);
    }

    // ==============================
    // UTILS
    // ==============================
    private Bounds CalculateBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private void NormalizePivot(GameObject obj)
    {
        Bounds b = CalculateBounds(obj);
        obj.transform.position -= b.center;
    }

    private float CalculateAutoCameraDistance(Bounds b, float fov)
    {
        float radius = b.extents.magnitude;
        float fovRad = fov * Mathf.Deg2Rad;
        return radius / Mathf.Tan(fovRad * 0.5f);
    }
}
