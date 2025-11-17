using System.Collections.Generic;
using UnityEngine;

public class BuildUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform parentTransform; // ScrollView Content
    [SerializeField] private BuildInfoUI buildInfoPrefab;
    [SerializeField] private BuildSlotUI slotItemPrefab;

    private readonly string[] arcTypeNames = { "기본", "바이오", "에너지", "기술" };

    private readonly Dictionary<int, BuildInfoUI> arcTypeUIs = new();

    private ArcDB arcDB;
    private ArcRecipeDB arcRecipeDB;

    private void Start()
    {
        arcDB = BuildManager.I.ArcDB;
        arcRecipeDB = BuildManager.I.RecipeDB;

        BuildAll();
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

            arcRecipeDB.TryGetRecipe(arcId, out var recipe);
            slotUI.Set(arc, recipe);
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
