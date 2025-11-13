using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager I;
    private void Awake() => I = this;


    private ArcDB arcDB;
    public ArcDB ArcDB => arcDB;
    private ArcRecipeDB recipeDB;
    public ArcRecipeDB RecipeDB => recipeDB;
    private DummyInventoryProvider inventory;

    public GameObject prefab;

    public void Init(ArcDB arcDB, ArcRecipeDB recipeDB, DummyInventoryProvider inventory)
    {
        this.arcDB = arcDB;
        this.recipeDB = recipeDB;
        this.inventory = inventory;
    }

    public bool TryBuild(int arcId, Vector3 pos, Quaternion rot)
    {
        if (!arcDB.TryGet(arcId, out var arc))
        {
            Debug.LogWarning($"없는 건물: {arcId}");
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

    bool HasMaterials(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
        {
            if (!inventory.HasItem(itemId, amount))
                return false;
        }
        return true;
    }

    void Consume(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
            inventory.Consume(itemId, amount);
    }

    bool Spawn(ArcData arc, Vector3 pos, Quaternion rot)
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
