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
    [SerializeField] private float maxHeightDiff = 0.1f;
    [SerializeField] private float maxSlopeAngle = 5f;

    private FlatSurfaceRule flatSurfaceRule;

    private void InitRules()
    {
        flatSurfaceRule = new FlatSurfaceRule(buildableLayer, maxHeightDiff, maxSlopeAngle);
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

        if (!flatSurfaceRule.Validate(ctx, out var errorCode))
        {
            Debug.Log($"건축 불가: {errorCode}");

            switch (errorCode)
            {
                case "NO_FLOOR":
                    toastMessageUI.Show("바닥이 없는 위치입니다.");
                    break;
                case "SLOPE_TOO_STEEP":
                    toastMessageUI.Show("경사가 너무 가파른 곳에는 설치할 수 없습니다.");
                    break;
                case "NOT_FLAT":
                    toastMessageUI.Show("평평한 지면에서만 설치할 수 있습니다.");
                    break;
                default:
                    toastMessageUI.Show("이 위치에는 건축할 수 없습니다.");
                    break;
            }

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
