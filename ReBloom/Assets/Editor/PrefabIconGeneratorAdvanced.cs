using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabIconGeneratorAdvanced : EditorWindow
{
    [MenuItem("Tools/Advanced Prefab Icon Generator")]
    static void Open() => GetWindow<PrefabIconGeneratorAdvanced>("Prefab Icon Generator");

    // ==============================
    // DATA
    // ==============================
    private List<PrefabIconProfile> profiles = new();
    private PrefabIconProfile currentProfile;
    private bool profileToRemove = false;
    private PrefabIconProfile pendingRemoval;

    // Preview Objects
    private GameObject previewInstance;
    private Camera previewCamera;
    private Light mainLight;
    private Light fillLight;
    private Light rimLight;
    private RenderTexture previewRT;

    private const int PREVIEW_SIZE = 256;
    private Vector2 scroll;
    private Vector2 windowScroll;
    // ==============================
    // OUTPUT SETTINGS
    // ==============================
    private int outputResolution = 256;
    private static readonly int[] RES_VALUES = { 128, 256, 512, 1024 };
    private static readonly string[] RES_LABELS = { "128", "256", "512", "1024" };

    // ==============================
    // VIEW PRESETS (Industry Standard)
    // ==============================
    private enum ViewPreset
    {
        Isometric,      // 30, 45, 0 - 가장 일반적인 아이콘 뷰
        ThreeQuarter,   // 30, 315, 0 - 3/4 뷰
        Front,          // 0, 0, 0
        Side,           // 0, 90, 0
        Top,            // 90, 0, 0
        DiabloStyle,    // 60, 45, 0 - 디아블로 스타일
        Custom
    }

    private static readonly Dictionary<ViewPreset, Vector3> PRESET_ROTATIONS = new()
    {
        { ViewPreset.Isometric, new Vector3(30f, 45f, 0f) },
        { ViewPreset.ThreeQuarter, new Vector3(30f, 315f, 0f) },
        { ViewPreset.Front, new Vector3(0f, 0f, 0f) },
        { ViewPreset.Side, new Vector3(0f, 90f, 0f) },
        { ViewPreset.Top, new Vector3(90f, 0f, 0f) },
        { ViewPreset.DiabloStyle, new Vector3(60f, 45f, 0f) },
    };

    private static readonly Dictionary<ViewPreset, float> PRESET_FOV = new()
    {
        { ViewPreset.Isometric, 30f },
        { ViewPreset.ThreeQuarter, 30f },
        { ViewPreset.Front, 30f },
        { ViewPreset.Side, 30f },
        { ViewPreset.Top, 30f },
        { ViewPreset.DiabloStyle, 20f },
    };

    // ==============================
    // LIGHTING PRESETS
    // ==============================
    private enum LightingPreset
    {
        Soft,           // 부드러운 조명
        Dramatic,       // 강한 대비
        Flat,           // 플랫 조명 (2D 느낌)
        ThreePoint,     // 표준 3점 조명
        Custom
    }

    // ==============================
    // OUTLINE SETTINGS
    // ==============================
    private bool enableOutline = false;
    private Color outlineColor = Color.black;
    private int outlineWidth = 2;

    // ==============================
    // BACKGROUND SETTINGS
    // ==============================
    private bool useTransparentBG = true;
    private Color backgroundColor = Color.clear;

    // ==============================
    // LIFECYCLE
    // ==============================
    private void OnEnable()
    {
        EditorApplication.update += RepaintWindow;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintWindow;
        Cleanup();
    }

    private void RepaintWindow()
    {
        if (currentProfile != null)
            Repaint();
    }

    // ==============================
    // MAIN GUI
    // ==============================
    private void OnGUI()
    {
        // Deferred removal (GUI 루프 밖에서 처리)
        if (profileToRemove && pendingRemoval != null)
        {
            ExecuteRemoveProfile(pendingRemoval);
            profileToRemove = false;
            pendingRemoval = null;
        }

        HandleDragAndDrop();

        // ===== 전체 스크롤뷰 시작 =====
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        EditorGUILayout.Space(5);
        DrawToolbar();

        EditorGUILayout.Space(5);
        DrawGlobalOutputSettings();

        EditorGUILayout.Space(5);
        DrawProfileList();

        if (currentProfile != null)
        {
            EditorGUILayout.Space(10);
            DrawViewPresets();

            EditorGUILayout.Space(5);
            DrawCameraSettings();

            EditorGUILayout.Space(5);
            DrawPrefabTransformSettings();

            EditorGUILayout.Space(5);
            DrawLightingSettings();

            EditorGUILayout.Space(5);
            DrawOutlineSettings();

            EditorGUILayout.Space(5);
            DrawBackgroundSettings();

            EditorGUILayout.Space(10);
            DrawPreview();

            EditorGUILayout.Space(5);
            DrawActionButtons();
        }
        else
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("프리팹을 드래그하여 추가하세요.", MessageType.Info);
        }

        //EditorGUILayout.Space(5);
        //DrawActionButtons();

        // ===== 전체 스크롤뷰 끝 =====
        EditorGUILayout.EndScrollView();
    }

    // ==============================
    // TOOLBAR
    // ==============================
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Batch Export All", EditorStyles.toolbarButton, GUILayout.Width(100)))
            BatchExport();

        if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            ClearAllProfiles();

        GUILayout.FlexibleSpace();
        GUILayout.Label("📦 Drag Prefabs Here", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // ==============================
    // OUTPUT SETTINGS
    // ==============================
    private void DrawGlobalOutputSettings()
    {
        EditorGUILayout.LabelField("📐 Output Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        outputResolution = EditorGUILayout.IntPopup("Resolution", outputResolution, RES_LABELS, RES_VALUES);

        EditorGUILayout.EndVertical();
    }

    // ==============================
    // PROFILE LIST
    // ==============================
    private void DrawProfileList()
    {
        EditorGUILayout.LabelField($"📋 Prefab List ({profiles.Count})", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));

        for (int i = 0; i < profiles.Count; i++)
        {
            var p = profiles[i];
            if (p == null || p.prefab == null) continue;

            EditorGUILayout.BeginHorizontal();

            // 선택 버튼
            GUI.color = (p == currentProfile) ? Color.cyan : Color.white;
            if (GUILayout.Button(p.prefab.name, GUILayout.ExpandWidth(true)))
                SelectProfile(p);
            GUI.color = Color.white;

            // 삭제 버튼
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                profileToRemove = true;
                pendingRemoval = p;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ==============================
    // VIEW PRESETS
    // ==============================
    private void DrawViewPresets()
    {
        EditorGUILayout.LabelField("🎯 View Presets", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Isometric\n(30°, 45°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.Isometric);

        if (GUILayout.Button("3/4 View\n(30°, 315°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.ThreeQuarter);

        if (GUILayout.Button("Front\n(0°, 0°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.Front);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Side\n(0°, 90°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.Side);

        if (GUILayout.Button("Top\n(90°, 0°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.Top);

        if (GUILayout.Button("Diablo Style\n(60°, 45°)", GUILayout.Height(40)))
            ApplyViewPreset(ViewPreset.DiabloStyle);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void ApplyViewPreset(ViewPreset preset)
    {
        if (currentProfile == null) return;

        currentProfile.cameraRotation = PRESET_ROTATIONS[preset];
        currentProfile.fov = PRESET_FOV[preset];
        EditorUtility.SetDirty(currentProfile);
        RebuildPreview();
    }

    // ==============================
    // CAMERA SETTINGS
    // ==============================
    private void DrawCameraSettings()
    {
        EditorGUILayout.LabelField("📷 Camera Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();

        currentProfile.cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", currentProfile.cameraRotation);
        currentProfile.fov = EditorGUILayout.Slider("FOV", currentProfile.fov, 10f, 60f);
        currentProfile.cameraDistance = EditorGUILayout.Slider("Distance Multiplier", currentProfile.cameraDistance, 0.5f, 3f);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
        }

        EditorGUILayout.EndVertical();
    }

    // ==============================
    // PREFAB TRANSFORM SETTINGS
    // ==============================
    private void DrawPrefabTransformSettings()
    {
        EditorGUILayout.LabelField("🔄 Prefab Transform", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();

        currentProfile.prefabRotation = EditorGUILayout.Vector3Field("Prefab Rotation", currentProfile.prefabRotation);
        currentProfile.prefabOffset = EditorGUILayout.Vector3Field("Prefab Offset", currentProfile.prefabOffset);
        currentProfile.prefabScale = EditorGUILayout.Slider("Prefab Scale", currentProfile.prefabScale, 0.1f, 3f);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
        }

        if (GUILayout.Button("Reset Transform"))
        {
            currentProfile.prefabRotation = Vector3.zero;
            currentProfile.prefabOffset = Vector3.zero;
            currentProfile.prefabScale = 1f;
            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
        }

        EditorGUILayout.EndVertical();
    }

    // ==============================
    // LIGHTING SETTINGS
    // ==============================
    private void DrawLightingSettings()
    {
        EditorGUILayout.LabelField("💡 Lighting Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Lighting Presets
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Soft"))
            ApplyLightingPreset(LightingPreset.Soft);
        if (GUILayout.Button("Dramatic"))
            ApplyLightingPreset(LightingPreset.Dramatic);
        if (GUILayout.Button("Flat"))
            ApplyLightingPreset(LightingPreset.Flat);
        if (GUILayout.Button("3-Point"))
            ApplyLightingPreset(LightingPreset.ThreePoint);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();

        // Main Light
        EditorGUILayout.LabelField("Main Light", EditorStyles.miniBoldLabel);
        currentProfile.mainLightRotation = EditorGUILayout.Vector3Field("  Rotation", currentProfile.mainLightRotation);
        currentProfile.mainLightIntensity = EditorGUILayout.Slider("  Intensity", currentProfile.mainLightIntensity, 0f, 3f);
        currentProfile.mainLightColor = EditorGUILayout.ColorField("  Color", currentProfile.mainLightColor);

        EditorGUILayout.Space(3);

        // Fill Light
        currentProfile.enableFillLight = EditorGUILayout.Toggle("Enable Fill Light", currentProfile.enableFillLight);
        if (currentProfile.enableFillLight)
        {
            currentProfile.fillLightIntensity = EditorGUILayout.Slider("  Intensity", currentProfile.fillLightIntensity, 0f, 2f);
            currentProfile.fillLightColor = EditorGUILayout.ColorField("  Color", currentProfile.fillLightColor);
        }

        EditorGUILayout.Space(3);

        // Rim Light
        currentProfile.enableRimLight = EditorGUILayout.Toggle("Enable Rim Light", currentProfile.enableRimLight);
        if (currentProfile.enableRimLight)
        {
            currentProfile.rimLightIntensity = EditorGUILayout.Slider("  Intensity", currentProfile.rimLightIntensity, 0f, 2f);
            currentProfile.rimLightColor = EditorGUILayout.ColorField("  Color", currentProfile.rimLightColor);
        }

        // Ambient Light
        EditorGUILayout.Space(3);
        currentProfile.ambientIntensity = EditorGUILayout.Slider("Ambient Intensity", currentProfile.ambientIntensity, 0f, 2f);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentProfile);
            UpdateLighting();
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyLightingPreset(LightingPreset preset)
    {
        if (currentProfile == null) return;

        switch (preset)
        {
            case LightingPreset.Soft:
                currentProfile.mainLightRotation = new Vector3(50f, 30f, 0f);
                currentProfile.mainLightIntensity = 1.0f;
                currentProfile.mainLightColor = Color.white;
                currentProfile.enableFillLight = true;
                currentProfile.fillLightIntensity = 0.5f;
                currentProfile.fillLightColor = new Color(0.8f, 0.9f, 1f);
                currentProfile.enableRimLight = false;
                currentProfile.ambientIntensity = 0.8f;
                break;

            case LightingPreset.Dramatic:
                currentProfile.mainLightRotation = new Vector3(45f, 45f, 0f);
                currentProfile.mainLightIntensity = 1.5f;
                currentProfile.mainLightColor = new Color(1f, 0.95f, 0.9f);
                currentProfile.enableFillLight = true;
                currentProfile.fillLightIntensity = 0.2f;
                currentProfile.fillLightColor = new Color(0.6f, 0.7f, 1f);
                currentProfile.enableRimLight = true;
                currentProfile.rimLightIntensity = 0.8f;
                currentProfile.rimLightColor = Color.white;
                currentProfile.ambientIntensity = 0.3f;
                break;

            case LightingPreset.Flat:
                currentProfile.mainLightRotation = new Vector3(0f, 0f, 0f);
                currentProfile.mainLightIntensity = 0.8f;
                currentProfile.mainLightColor = Color.white;
                currentProfile.enableFillLight = true;
                currentProfile.fillLightIntensity = 0.8f;
                currentProfile.fillLightColor = Color.white;
                currentProfile.enableRimLight = false;
                currentProfile.ambientIntensity = 1.5f;
                break;

            case LightingPreset.ThreePoint:
                currentProfile.mainLightRotation = new Vector3(45f, 45f, 0f);
                currentProfile.mainLightIntensity = 1.2f;
                currentProfile.mainLightColor = Color.white;
                currentProfile.enableFillLight = true;
                currentProfile.fillLightIntensity = 0.4f;
                currentProfile.fillLightColor = new Color(0.9f, 0.9f, 1f);
                currentProfile.enableRimLight = true;
                currentProfile.rimLightIntensity = 0.6f;
                currentProfile.rimLightColor = Color.white;
                currentProfile.ambientIntensity = 0.5f;
                break;
        }

        EditorUtility.SetDirty(currentProfile);
        UpdateLighting();
    }

    // ==============================
    // OUTLINE SETTINGS
    // ==============================
    private void DrawOutlineSettings()
    {
        EditorGUILayout.LabelField("✏️ Outline Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        enableOutline = EditorGUILayout.Toggle("Enable Outline", enableOutline);

        if (enableOutline)
        {
            outlineColor = EditorGUILayout.ColorField("Outline Color", outlineColor);
            outlineWidth = EditorGUILayout.IntSlider("Outline Width", outlineWidth, 1, 10);
        }

        EditorGUILayout.EndVertical();
    }

    // ==============================
    // BACKGROUND SETTINGS
    // ==============================
    private void DrawBackgroundSettings()
    {
        EditorGUILayout.LabelField("🎨 Background", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        useTransparentBG = EditorGUILayout.Toggle("Transparent Background", useTransparentBG);

        if (!useTransparentBG)
        {
            backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        }

        if (previewCamera != null)
        {
            previewCamera.backgroundColor = useTransparentBG ? Color.clear : backgroundColor;
        }

        EditorGUILayout.EndVertical();
    }

    // ==============================
    // PREVIEW
    // ==============================
    private void DrawPreview()
    {
        EditorGUILayout.LabelField("👁️ Preview (Drag to Rotate Camera)", EditorStyles.boldLabel);

        // 체커보드 배경으로 투명도 표시
        Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE, GUILayout.ExpandWidth(false));

        // 체커보드 패턴 그리기 (투명 배경 시각화)
        if (useTransparentBG)
        {
            DrawCheckerboard(previewRect);
        }

        if (previewRT != null)
        {
            GUI.DrawTexture(previewRect, previewRT, ScaleMode.ScaleToFit);
        }

        HandlePreviewMouseInput(previewRect);
    }

    private void DrawCheckerboard(Rect rect)
    {
        int checkerSize = 10;
        Color c1 = new Color(0.3f, 0.3f, 0.3f);
        Color c2 = new Color(0.4f, 0.4f, 0.4f);

        Texture2D checkerTex = new Texture2D(checkerSize * 2, checkerSize * 2);
        for (int y = 0; y < checkerSize * 2; y++)
        {
            for (int x = 0; x < checkerSize * 2; x++)
            {
                bool isWhite = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                checkerTex.SetPixel(x, y, isWhite ? c1 : c2);
            }
        }
        checkerTex.Apply();

        GUI.DrawTextureWithTexCoords(rect, checkerTex, new Rect(0, 0, rect.width / checkerSize, rect.height / checkerSize));
        DestroyImmediate(checkerTex);
    }

    private void HandlePreviewMouseInput(Rect rect)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            // 카메라 회전
            currentProfile.cameraRotation.y += e.delta.x * 0.5f;
            currentProfile.cameraRotation.x -= e.delta.y * 0.5f;
            currentProfile.cameraRotation.x = Mathf.Clamp(currentProfile.cameraRotation.x, -90f, 90f);

            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 1)
        {
            // 프리팹 회전 (우클릭 드래그)
            currentProfile.prefabRotation.y += e.delta.x * 0.5f;
            currentProfile.prefabRotation.x -= e.delta.y * 0.5f;

            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
            e.Use();
        }
        else if (e.type == EventType.ScrollWheel)
        {
            // 줌
            currentProfile.cameraDistance += e.delta.y * 0.05f;
            currentProfile.cameraDistance = Mathf.Clamp(currentProfile.cameraDistance, 0.5f, 3f);

            EditorUtility.SetDirty(currentProfile);
            RebuildPreview();
            e.Use();
        }
    }

    // ==============================
    // ACTION BUTTONS
    // ==============================
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.green;
        if (GUILayout.Button("💾 Save Icon", GUILayout.Height(30)))
            SaveScreenshot();
        GUI.color = Color.white;

        if (GUILayout.Button("📋 Copy Settings", GUILayout.Height(30)))
            CopySettingsToClipboard();

        if (GUILayout.Button("📥 Paste Settings", GUILayout.Height(30)))
            PasteSettingsFromClipboard();

        EditorGUILayout.EndHorizontal();
    }

    // ==============================
    // DRAG & DROP
    // ==============================
    private void HandleDragAndDrop()
    {
        Event e = Event.current;

        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    CreateProfile(go);
                }
            }
        }

        e.Use();
    }

    // ==============================
    // PROFILE MANAGEMENT
    // ==============================
    private void CreateProfile(GameObject prefab)
    {
        if (profiles.Exists(p => p != null && p.prefab == prefab))
            return;

        var profile = ScriptableObject.CreateInstance<PrefabIconProfile>();
        profile.prefab = prefab;
        profile.InitializeDefaults();

        // 폴더 확인 및 생성
        if (!AssetDatabase.IsValidFolder("Assets/IconProfiles"))
            AssetDatabase.CreateFolder("Assets", "IconProfiles");

        string path = $"Assets/IconProfiles/{prefab.name}_IconProfile.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        profiles.Add(profile);
        SelectProfile(profile);
    }

    private void SelectProfile(PrefabIconProfile profile)
    {
        if (currentProfile == profile) return;

        currentProfile = profile;
        RebuildPreview();
    }

    private void ExecuteRemoveProfile(PrefabIconProfile profile)
    {
        if (currentProfile == profile)
        {
            currentProfile = null;
            Cleanup();
        }

        profiles.Remove(profile);

        string path = AssetDatabase.GetAssetPath(profile);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
    }

    private void ClearAllProfiles()
    {
        if (!EditorUtility.DisplayDialog("Clear All", "모든 프로필을 삭제하시겠습니까?", "Yes", "No"))
            return;

        foreach (var p in profiles)
        {
            string path = AssetDatabase.GetAssetPath(p);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);
        }

        profiles.Clear();
        currentProfile = null;
        Cleanup();
        AssetDatabase.SaveAssets();
    }

    // ==============================
    // PREVIEW SYSTEM
    // ==============================
    private void RebuildPreview()
    {
        Cleanup();

        if (currentProfile == null || currentProfile.prefab == null)
            return;

        // 프리뷰 인스턴스 생성
        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(currentProfile.prefab);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.transform.position = Vector3.zero;

        // 프리팹 트랜스폼 적용
        previewInstance.transform.rotation = Quaternion.Euler(currentProfile.prefabRotation);
        previewInstance.transform.localScale = Vector3.one * currentProfile.prefabScale;

        // 바운드 계산 및 중심점 조정
        Bounds bounds = CalculateBounds(previewInstance);
        Vector3 center = bounds.center + currentProfile.prefabOffset;
        previewInstance.transform.position -= center;
        bounds = CalculateBounds(previewInstance);

        // 카메라 생성
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = useTransparentBG ? Color.clear : backgroundColor;
        previewCamera.fieldOfView = currentProfile.fov;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 1000f;

        // 카메라 위치 계산
        float distance = CalculateCameraDistance(bounds, currentProfile.fov) * currentProfile.cameraDistance;
        Quaternion camRotation = Quaternion.Euler(currentProfile.cameraRotation);
        Vector3 camDirection = camRotation * Vector3.forward;
        previewCamera.transform.position = bounds.center - camDirection * distance;
        previewCamera.transform.LookAt(bounds.center);

        // 렌더 텍스처
        previewRT = new RenderTexture(PREVIEW_SIZE, PREVIEW_SIZE, 24, RenderTextureFormat.ARGB32);
        previewCamera.targetTexture = previewRT;

        // 조명 설정
        SetupLighting(bounds.center);

        // 렌더링
        previewCamera.Render();
    }

    private void SetupLighting(Vector3 targetCenter)
    {
        // Main Light
        GameObject mainLightObj = new GameObject("MainLight");
        mainLightObj.hideFlags = HideFlags.HideAndDontSave;
        mainLight = mainLightObj.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.transform.rotation = Quaternion.Euler(currentProfile.mainLightRotation);
        mainLight.intensity = currentProfile.mainLightIntensity;
        mainLight.color = currentProfile.mainLightColor;

        // Fill Light
        if (currentProfile.enableFillLight)
        {
            GameObject fillLightObj = new GameObject("FillLight");
            fillLightObj.hideFlags = HideFlags.HideAndDontSave;
            fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.transform.rotation = Quaternion.Euler(currentProfile.mainLightRotation.x, currentProfile.mainLightRotation.y + 180f, 0f);
            fillLight.intensity = currentProfile.fillLightIntensity;
            fillLight.color = currentProfile.fillLightColor;
        }

        // Rim Light
        if (currentProfile.enableRimLight)
        {
            GameObject rimLightObj = new GameObject("RimLight");
            rimLightObj.hideFlags = HideFlags.HideAndDontSave;
            rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.transform.rotation = Quaternion.Euler(-20f, currentProfile.mainLightRotation.y + 180f, 0f);
            rimLight.intensity = currentProfile.rimLightIntensity;
            rimLight.color = currentProfile.rimLightColor;
        }

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = currentProfile.ambientIntensity;
    }

    private void UpdateLighting()
    {
        if (currentProfile == null) return;
        RebuildPreview();
    }

    private void Cleanup()
    {
        if (previewInstance != null) DestroyImmediate(previewInstance);
        if (previewCamera != null) DestroyImmediate(previewCamera.gameObject);
        if (mainLight != null) DestroyImmediate(mainLight.gameObject);
        if (fillLight != null) DestroyImmediate(fillLight.gameObject);
        if (rimLight != null) DestroyImmediate(rimLight.gameObject);
        if (previewRT != null)
        {
            previewRT.Release();
            DestroyImmediate(previewRT);
        }

        previewInstance = null;
        previewCamera = null;
        mainLight = null;
        fillLight = null;
        rimLight = null;
        previewRT = null;
    }

    // ==============================
    // EXPORT
    // ==============================
    private void SaveScreenshot()
    {
        string folder = EditorUtility.SaveFolderPanel("Save Icon", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        Texture2D result = RenderToTexture(outputResolution);

        if (enableOutline)
            result = ApplyOutline(result, outlineColor, outlineWidth);

        string path = System.IO.Path.Combine(folder, currentProfile.prefab.name + "_icon.png");
        System.IO.File.WriteAllBytes(path, result.EncodeToPNG());
        DestroyImmediate(result);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Icon saved:\n{path}", "OK");
    }

    private void BatchExport()
    {
        if (profiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No profiles to export.", "OK");
            return;
        }

        string folder = EditorUtility.SaveFolderPanel("Batch Export Icons", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        var originalProfile = currentProfile;

        foreach (var profile in profiles)
        {
            if (profile == null || profile.prefab == null) continue;

            currentProfile = profile;
            RebuildPreview();

            Texture2D result = RenderToTexture(outputResolution);

            if (enableOutline)
                result = ApplyOutline(result, outlineColor, outlineWidth);

            string path = System.IO.Path.Combine(folder, profile.prefab.name + "_icon.png");
            System.IO.File.WriteAllBytes(path, result.EncodeToPNG());
            DestroyImmediate(result);
        }

        currentProfile = originalProfile;
        if (currentProfile != null)
            RebuildPreview();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Batch Export", $"Exported {profiles.Count} icons.", "OK");
    }
    private Texture2D RenderToTexture(int resolution)
    {
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        previewCamera.targetTexture = rt;

        if (useTransparentBG)
        {
            // === 흰 배경으로 렌더링 후 배경 제거 방식 ===

            // 1. 흰 배경으로 렌더링
            previewCamera.backgroundColor = Color.white;
            previewCamera.Render();

            Texture2D texWhite = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            texWhite.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            texWhite.Apply();

            // 2. 검은 배경으로 렌더링
            previewCamera.backgroundColor = Color.black;
            previewCamera.Render();

            Texture2D texBlack = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            texBlack.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            texBlack.Apply();

            // 3. 두 이미지로 알파 계산
            Texture2D result = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] whitePixels = texWhite.GetPixels();
            Color[] blackPixels = texBlack.GetPixels();
            Color[] final = new Color[whitePixels.Length];

            for (int i = 0; i < whitePixels.Length; i++)
            {
                Color w = whitePixels[i];
                Color b = blackPixels[i];

                // 알파 계산: 흰 배경과 검은 배경의 차이로 계산
                // 공식: alpha = 1 - (white - black)
                float alphaR = 1f - (w.r - b.r);
                float alphaG = 1f - (w.g - b.g);
                float alphaB = 1f - (w.b - b.b);
                float alpha = (alphaR + alphaG + alphaB) / 3f;
                alpha = Mathf.Clamp01(alpha);

                if (alpha < 0.01f)
                {
                    // 완전 투명
                    final[i] = Color.clear;
                }
                else if (alpha > 0.99f)
                {
                    // 완전 불투명 - 검은 배경 이미지 사용 (원본 색상)
                    final[i] = new Color(b.r, b.g, b.b, 1f);
                }
                else
                {
                    // 반투명 - 원본 색상 복원
                    float r = Mathf.Clamp01(b.r / alpha);
                    float g = Mathf.Clamp01(b.g / alpha);
                    float bl = Mathf.Clamp01(b.b / alpha);
                    final[i] = new Color(r, g, bl, alpha);
                }
            }

            result.SetPixels(final);
            result.Apply();

            // 정리
            DestroyImmediate(texWhite);
            DestroyImmediate(texBlack);
            RenderTexture.active = null;
            previewCamera.targetTexture = previewRT;
            previewCamera.backgroundColor = Color.clear;
            rt.Release();
            DestroyImmediate(rt);

            return result;
        }
        else
        {
            // === 배경색 그대로 사용 ===
            previewCamera.backgroundColor = backgroundColor;
            previewCamera.Render();

            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            previewCamera.targetTexture = previewRT;
            rt.Release();
            DestroyImmediate(rt);

            return tex;
        }
    }

    // ==============================
    // OUTLINE
    // ==============================
    private Texture2D ApplyOutline(Texture2D source, Color outlineCol, int width)
    {
        int w = source.width;
        int h = source.height;
        Color[] pixels = source.GetPixels();
        Color[] result = new Color[pixels.Length];
        System.Array.Copy(pixels, result, pixels.Length);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (pixels[idx].a > 0.1f) continue; // 이미 픽셀이 있으면 스킵

                // 주변에 불투명 픽셀이 있는지 확인
                bool hasNeighbor = false;
                for (int dy = -width; dy <= width && !hasNeighbor; dy++)
                {
                    for (int dx = -width; dx <= width && !hasNeighbor; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                        int nIdx = ny * w + nx;
                        if (pixels[nIdx].a > 0.5f)
                        {
                            hasNeighbor = true;
                        }
                    }
                }

                if (hasNeighbor)
                {
                    result[idx] = outlineCol;
                }
            }
        }

        Texture2D outlineTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outlineTex.SetPixels(result);
        outlineTex.Apply();

        DestroyImmediate(source);
        return outlineTex;
    }

    // ==============================
    // CLIPBOARD (Settings Copy/Paste)
    // ==============================
    private static PrefabIconProfile clipboardProfile;

    private void CopySettingsToClipboard()
    {
        if (currentProfile == null) return;

        clipboardProfile = ScriptableObject.CreateInstance<PrefabIconProfile>();
        EditorUtility.CopySerialized(currentProfile, clipboardProfile);
        clipboardProfile.prefab = null; // 프리팹 참조는 복사하지 않음

        EditorUtility.DisplayDialog("Copied", "Settings copied to clipboard.", "OK");
    }

    private void PasteSettingsFromClipboard()
    {
        if (currentProfile == null || clipboardProfile == null)
        {
            EditorUtility.DisplayDialog("Warning", "No settings in clipboard.", "OK");
            return;
        }

        var prefab = currentProfile.prefab; // 현재 프리팹 보존
        EditorUtility.CopySerialized(clipboardProfile, currentProfile);
        currentProfile.prefab = prefab; // 프리팹 복원

        EditorUtility.SetDirty(currentProfile);
        RebuildPreview();
    }

    // ==============================
    // UTILITIES
    // ==============================
    private Bounds CalculateBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private float CalculateCameraDistance(Bounds bounds, float fov)
    {
        float radius = bounds.extents.magnitude;
        float fovRad = fov * Mathf.Deg2Rad;
        return radius / Mathf.Tan(fovRad * 0.5f) * 1.2f;
    }
}