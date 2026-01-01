using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GreeningVisualController : MonoBehaviour
{
    [Header("BG")]
    [SerializeField] private bool loadFromBGOnAwake = true;

    [Header("Scene Roots (children names must match keys)")]
    [SerializeField] private Transform planetRoot;
    [SerializeField] private Transform animalRoot;
    [SerializeField] private Transform insectRoot;

    [Header("Terrain")]
    [SerializeField] private Terrain targetTerrain;
    [Tooltip("Terrain 머티리얼 색 프로퍼티 후보들 (첫 번째로 찾은 걸 사용)")]
    [SerializeField] private string[] terrainColorProperties = { "_BaseColor", "_Color", "_TintColor" };

    [Header("Fog Volume (inGameFog)")]
    [SerializeField] private GameObject inGameFog;
    [SerializeField] private bool controlInGameFogVolume = true;
    [SerializeField] private float fogFadeSpeed = 2f;     // 100% 도달 시 페이드아웃 속도
    [SerializeField] private bool fadeOutAt100 = true;    // true면 SetActive 대신 weight로 페이드

    private Volume inGameFogVolume;
    private ColorAdjustments inGameFogColorAdj;
    private float fogTargetWeight = 1f;


    private readonly Dictionary<string, GameObject> planetByKey = new();
    private readonly Dictionary<string, GameObject> animalByKey = new();
    private readonly Dictionary<string, GameObject> insectByKey = new();

    private GreeningDB greeningDb;

    private Material terrainMat;
    private string terrainColorPropInUse;

    private void Awake()
    {
        // DB 준비
        greeningDb = new GreeningDB();
        if (loadFromBGOnAwake)
        {
            greeningDb.LoadFromBG(autoComputeMinGreening: true);
        }

        CacheDirectChildrenByName(planetRoot, planetByKey);
        CacheDirectChildrenByName(animalRoot, animalByKey);
        CacheDirectChildrenByName(insectRoot, insectByKey);

        if (targetTerrain != null)
        {
            terrainMat = targetTerrain.materialTemplate;
            terrainColorPropInUse = FindFirstExistingColorProperty(terrainMat, terrainColorProperties);
        }

        ForceDisableAll(planetByKey);
        ForceDisableAll(animalByKey);
        ForceDisableAll(insectByKey);

        //CacheInGameFogVolume();
    }

    private async void Start()
    {
        if (loadFromBGOnAwake)
            await UniTask.WaitUntil(() => greeningDb != null && greeningDb.IsLoaded);

        await UniTask.WaitUntil(() => ResearchManager.I != null);

        ResearchManager.I.OnGreeningChanged -= HandleGreeningChanged;
        ResearchManager.I.OnGreeningChanged += HandleGreeningChanged;

        HandleGreeningChanged(ResearchManager.I.CurrentGreening);
    }

    private void OnDestroy()
    {
        if (ResearchManager.I != null)
            ResearchManager.I.OnGreeningChanged -= HandleGreeningChanged;
    }

    public void ForceDisableInGameFog()
    {
        if (inGameFog == null) return;

        if (!controlInGameFogVolume || inGameFogVolume == null || !fadeOutAt100)
        {
            inGameFog.SetActive(false);
            return;
        }

        fogTargetWeight = 0f;
        if (!inGameFog.activeSelf) inGameFog.SetActive(true);
    }


    /// <summary>
    /// 저장/로드로 ResearchManager 값이 먼저 세팅되거나, DB가 먼저 로드되거나 순서가 섞여도
    /// 최종적으로 현재 greening을 1회 적용
    /// </summary>
    private void TryApplyCurrentGreening()
    {
        if (ResearchManager.I == null) return;
        if (greeningDb == null) return;

        if (loadFromBGOnAwake && !greeningDb.IsLoaded) return;

        HandleGreeningChanged(ResearchManager.I.CurrentGreening);
    }

    private void Update()
    {
        if (controlInGameFogVolume && inGameFogVolume != null && fadeOutAt100)
        {
            inGameFogVolume.weight = Mathf.MoveTowards(
                inGameFogVolume.weight,
                fogTargetWeight,
                fogFadeSpeed * Time.deltaTime
            );

            if (fogTargetWeight <= 0f && inGameFogVolume.weight <= 0.0001f)
            {
                if (inGameFog != null) inGameFog.SetActive(false);
            }
        }
    }


    private readonly HashSet<string> enabledPlanets = new();
    private readonly HashSet<string> enabledAnimals = new();
    private readonly HashSet<string> enabledInsects = new();

    private void HandleGreeningChanged(float greening)
    {
        if (greeningDb == null) return;
        if (loadFromBGOnAwake && !greeningDb.IsLoaded) return;

        int stageIndex = greeningDb.GetStageIndex(greening);
        if (stageIndex < 0) return;

        ApplyCumulative(stageIndex);

        var row = greeningDb.SortedRows[stageIndex];
        ApplyTerrainColor(row.terrainColor);

        bool disableFog = greening >= 100f;
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.SetGreeningFog(row.fogColor, disableFog);
        //ApplyInGameFogVolume(row.fogColor, disableFog);
    }

    private void ApplyInGameFogVolume(Color fogColor, bool disableFog)
    {
        if (!controlInGameFogVolume || inGameFogVolume == null || inGameFogColorAdj == null)
        {
            if (inGameFog != null) inGameFog.SetActive(!disableFog);
            return;
        }

        inGameFogColorAdj.active = true;
        inGameFogColorAdj.colorFilter.overrideState = true;
        inGameFogColorAdj.colorFilter.value = fogColor;

        if (!fadeOutAt100)
        {
            if (inGameFog != null) inGameFog.SetActive(!disableFog);
            return;
        }

        fogTargetWeight = disableFog ? 0f : 1f;

        if (inGameFog != null && !inGameFog.activeSelf)
            inGameFog.SetActive(true);
    }


    private void CacheInGameFogVolume()
    {
        if (inGameFog == null) return;

        inGameFogVolume = inGameFog.GetComponent<Volume>();
        if (inGameFogVolume == null)
        {
            Debug.LogWarning("[GreeningVisual] inGameFog has no Volume component.");
            return;
        }

        if (inGameFogVolume.sharedProfile != null && inGameFogVolume.profile == null)
            inGameFogVolume.profile = Instantiate(inGameFogVolume.sharedProfile);

        var profile = inGameFogVolume.profile != null ? inGameFogVolume.profile : inGameFogVolume.sharedProfile;
        if (profile == null) return;

        if (!profile.TryGet<ColorAdjustments>(out inGameFogColorAdj))
            Debug.LogWarning("[GreeningVisual] inGameFog VolumeProfile has no ColorAdjustments override.");
    }

    private void ApplyCumulative(int stageIndex)
    {
        var rows = greeningDb.SortedRows;

        for (int i = 0; i <= stageIndex; i++)
        {
            var r = rows[i];

            EnableIfNeeded(planetByKey, enabledPlanets, r.planetKey, "Planet");
            EnableIfNeeded(animalByKey, enabledAnimals, r.animalKey, "Animal");
            EnableIfNeeded(insectByKey, enabledInsects, r.insectKey, "Insect");
        }
    }

    private static bool IsZero(string s) => string.IsNullOrWhiteSpace(s) || s.Trim() == "0";

    private static void EnableIfNeeded(
        Dictionary<string, GameObject> dict,
        HashSet<string> enabledSet,
        string key,
        string label)
    {
        if (IsZero(key)) return;

        key = key.Trim();
        if (enabledSet.Contains(key)) return;

        if (dict.TryGetValue(key, out var go) && go != null)
        {
            go.SetActive(true);
            enabledSet.Add(key);
            Debug.Log($"[GreeningVisual] Enabled {label}: {key}");
        }
        else
        {
            Debug.LogWarning($"[GreeningVisual] {label} key not found: '{key}' (check BG string vs GameObject name)");
        }
    }

    // -----------------------
    // Object switch helpers
    // -----------------------

    private static void CacheDirectChildrenByName(Transform root, Dictionary<string, GameObject> dict)
    {
        dict.Clear();
        if (root == null) return;

        for (int i = 0; i < root.childCount; i++)
        {
            var go = root.GetChild(i).gameObject;
            if (!dict.ContainsKey(go.name))
                dict.Add(go.name, go);
        }
    }


    private static void ForceDisableAll(Dictionary<string, GameObject> dict)
    {
        foreach (var kv in dict)
        {
            if (kv.Value != null)
                kv.Value.SetActive(false);
        }
    }

    // -----------------------
    // Color apply helpers
    // -----------------------

    private static string FindFirstExistingColorProperty(Material mat, string[] candidates)
    {
        if (mat == null || candidates == null) return null;
        foreach (var p in candidates)
        {
            if (!string.IsNullOrWhiteSpace(p) && mat.HasProperty(p))
                return p;
        }
        return null;
    }

    private void ApplyTerrainColor(Color c)
    {
        if (terrainMat == null) return;

        if (!string.IsNullOrWhiteSpace(terrainColorPropInUse))
        {
            terrainMat.SetColor(terrainColorPropInUse, c);
        }
        else
        {
            // URP Terrain Lit 기본 머티리얼은 Tint 컬러가 없을 수 있음.
            // 이 경우: 커스텀 Terrain 머티리얼(ShaderGraph)에 _TintColor 같은 프로퍼티를 만들어서 쓰는 걸 추천.
            // (여기서는 경고만)
            // Debug.LogWarning("[GreeningVisual] No color property found on terrain material. Add a tint color property or use custom shader.");
        }
    }
}
