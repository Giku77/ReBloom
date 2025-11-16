using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager I;
    private void Awake() => I = this;

    private BuildingFootprintProvider footprintProvider;
    private ToastMessageUI toastMessageUI;

    private ArcDB arcDB;
    public ArcDB ArcDB => arcDB;
    private ArcRecipeDB recipeDB;
    public ArcRecipeDB RecipeDB => recipeDB;
    private GameInventory inventory;

    public GameObject prefab;

    [Header("Build Rules")]
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float maxHeightDiff = 0.1f;
    [SerializeField] private float maxSlopeAngle = 5f;

    private List<IBuildRule> buildRules = new List<IBuildRule>();

    private FlatSurfaceRule flatSurfaceRule;

    private Dictionary <int, int> buildingCounts = new Dictionary<int, int>();

    public int GetCount(int arcId)
    {
        if (buildingCounts.TryGetValue(arcId, out var count))
            return count;
        return 0;
    }

    private void InitRules()
    {
        buildRules.Add(new FlatSurfaceRule(buildableLayer, maxHeightDiff, maxSlopeAngle));
        buildRules.Add(new CollisionRule(obstacleLayer));
        buildRules.Add(new LimitRule(this));
    }

    public void Init(ArcDB arcDB, ArcRecipeDB recipeDB, GameInventory inventory)
    {
        InitRules();
        this.arcDB = arcDB;
        this.recipeDB = recipeDB;
        this.inventory = inventory;
        footprintProvider = GetComponent<BuildingFootprintProvider>();
        toastMessageUI = GameObject.FindWithTag("ToastMsg").GetComponent<ToastMessageUI>();
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        foreach (var rule in buildRules)
        {
            if (!rule.Validate(ctx, out errorCode))
                return false;
        }
        errorCode = null;
        return true;
    }

    public bool TryBuild(int arcId, Vector3 pos, Quaternion rot)
    {
        if (!arcDB.TryGet(arcId, out var arc))
        {
            Debug.LogWarning($"없는 건물: {arcId}");
            return false;
        }

        var ctx = new ArcContext
        {
            Data = arc,
            Position = pos,
            Rotation = rot,
            FootPrint = footprintProvider.GetFootprint(),
            PlayerTransform = GameObject.FindWithTag("Player").transform
        };

        if (!Validate(ctx, out var errorCode))
        {
            toastMessageUI.Show($"건물 설치 불가: {errorCode}");
            return false;
        }

        if (!recipeDB.TryGetRecipe(arcId, out var recipe))
        {
            Debug.LogWarning($"건물 {arcId} 는 레시피가 없음. 테스트용으로 그냥 짓기");
            return Spawn(arc, pos, rot);
        }

        if (!HasMaterials(recipe))
            return false;

        Consume(recipe);
        if (buildingCounts.ContainsKey(arc.arcId))
            buildingCounts[arc.arcId]++;
        else
            buildingCounts[arc.arcId] = 1;
        QuestManager.I.NotifyBuildingBuilt(arc.arcId);

        return Spawn(arc, pos, rot);
    }

    private bool HasMaterials(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
        {
            if (!inventory.HasItem(itemId, amount))
                return false;
        }
        return true;
    }

    private void Consume(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
            inventory.Consume(itemId, amount);
    }

    private bool Spawn(ArcData arc, Vector3 pos, Quaternion rot)
    {
        //var prefab = Resources.Load<GameObject>($"Arc/{arc.arcId}");
        if (prefab == null)
        {
            Debug.LogError($"프리팹 없음: {arc.arcId}");
            return false;
        }
        Instantiate(prefab, pos, rot);
        return true;
    }
}
