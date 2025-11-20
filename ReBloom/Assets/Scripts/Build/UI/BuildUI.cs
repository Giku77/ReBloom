using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform parentTransform; // ScrollView Content
    [SerializeField] private BuildInfoUI buildInfoPrefab;
    [SerializeField] private BuildSlotUI slotItemPrefab;
    [SerializeField] private BuildToolTip toolTip;
    [SerializeField] private TextMeshProUGUI researchPoint;

    private readonly string[] arcTypeNames = { "기본", "바이오", "에너지", "기술" };

    private readonly Dictionary<int, BuildInfoUI> arcTypeUIs = new();
    private readonly Dictionary<int, BuildSlotUI> slotUIsByArcId = new();

    private ArcDB arcDB;
    private ArcRecipeDB arcRecipeDB;

    private void OnEnable()
    {
        if (ResearchManager.I != null) ResearchManager.I.OnProgressChanged += UpdateResearchPointDisplay;
         if (arcDB != null)
        {
            UpdateResearchPointDisplay(ResearchManager.I.CurrentProgress);
        }
    }

    private void OnDisable()
    {
        if (ResearchManager.I != null) ResearchManager.I.OnProgressChanged -= UpdateResearchPointDisplay;
    }

    private void Start()
    {
        arcDB = BuildManager.I.ArcDB;
        arcRecipeDB = BuildManager.I.RecipeDB;

        Debug.Log($"[BuildUI] BuildManager.I: {BuildManager.I}");
        Debug.Log($"[BuildUI] BuildManager.I?.ArcDB: {BuildManager.I?.ArcDB}");

        arcDB = BuildManager.I.ArcDB;
        arcRecipeDB = BuildManager.I.RecipeDB;

        if (arcDB == null)
        {
            Debug.LogError("[BuildUI] ArcDB 가 아직 초기화되지 않았습니다. BuildManager.Init 이 먼저 호출되어야 합니다.");
            return;
        }

        BuildAll();
        Toggle();
        UpdateResearchPointDisplay(ResearchManager.I.CurrentProgress);
    }

    private void UpdateResearchPointDisplay(float p)
    {
        //researchPoint.text = $"{p:F0}"; // 반올림
        researchPoint.text = Mathf.FloorToInt(p).ToString(); // 버림
        RefreshUnlockStates();
    }

    private void RefreshUnlockStates()
    {
        if (arcDB == null) return;

        foreach (var pair in slotUIsByArcId)
        {
            int arcId = pair.Key;
            BuildSlotUI slotUI = pair.Value;
            if (slotUI == null) continue;

            if (!arcDB.GetAll().TryGetValue(arcId, out var arc))
                continue;

            bool unlocked = ResearchManager.I.IsUnlocked(arc);

            slotUI.UpdateUnlockState(unlocked);
        }
    }

    private void BuildAll()
    {
        foreach (var pair in arcDB.GetAll())
        {
            int arcId = pair.Key;
            ArcData arc = pair.Value;

            // 1) 타입 그룹 UI 없으면 생성
            if (!arcTypeUIs.TryGetValue(arc.arcType, out var infoUI))
            {
                infoUI = Instantiate(buildInfoPrefab, parentTransform);
                infoUI.SetTypeName(GetArcTypeName(arc.arcType));
                arcTypeUIs.Add(arc.arcType, infoUI);
            }

            // 2) 슬롯 생성 및 세팅      
            var slotUI = Instantiate(slotItemPrefab, infoUI.SlotParent);

            bool unlocked = ResearchManager.I.IsUnlocked(arc);

            arcRecipeDB.TryGetRecipe(arcId, out var recipe);
            slotUI.Set(arc, recipe, unlocked);
            slotUI.Init(toolTip);

            slotUIsByArcId[arcId] = slotUI;
        }
    }

    private string GetArcTypeName(int arcType)
    {
        int index = Mathf.Clamp(arcType - 1, 0, arcTypeNames.Length - 1);
        return arcTypeNames[index];
    }

    public void Toggle()
    {
        bool next = !gameObject.activeSelf;
        gameObject.SetActive(next);

        Cursor.visible = next;
        Cursor.lockState = next ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
