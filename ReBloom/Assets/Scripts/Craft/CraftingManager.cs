using System.Collections.Generic;

public enum CraftFailReason
{
    None = 0,
    RecipeNotFound,
    MissingRequiredArc,   // 필요 아크(건축물) 없음
    LockedByResearch,     // 잠금 상태 (연구/진행도 부족 등)
    NotEnoughMaterials,
    NoOutputSpace         // 인벤토리에 공간 없음
}

public struct CraftCheckResult
{
    public bool canCraft;
    public CraftFailReason failReason;
    public CraftRecipeData recipe;

    public Dictionary<int, int> missingMaterials; // itemId -> 부족 개수
}

public class CraftingManager
{
    private readonly CraftRecipeDB _recipeDb;
    private readonly IInventoryProvider _inventory;

    public CraftingManager(CraftRecipeDB recipeDb, IInventoryProvider inventory)
    {
        _recipeDb = recipeDb;
        _inventory  = inventory;
    }

    private bool HasRequiredArcs(CraftRecipeData recipe)
    {
        return true;
    }

    public CraftCheckResult CanCraft(int recipeId)
    {
        var result = new CraftCheckResult
        {
            canCraft = false,
            failReason = CraftFailReason.None,
            missingMaterials = new Dictionary<int, int>()
        };

        if (!_recipeDb.TryGet(recipeId, out var recipe))
        {
            result.failReason = CraftFailReason.RecipeNotFound;
            return result;
        }

        result.recipe = recipe;

        // TODO: 필요 아크/연구 조건 체크
        // if (!HasRequiredArcs(recipe)) { ... }

        bool hasAllMaterials = true;
        foreach (var mat in recipe.materials)
        {
            int have = _inventory.GetItemCount(mat.itemId);
            int need = mat.count;

            if (have < need)
            {
                hasAllMaterials = false;
                result.missingMaterials[mat.itemId] = need - have;
            }
        }

        if (!hasAllMaterials)
        {
            result.failReason = CraftFailReason.NotEnoughMaterials;
            return result;
        }

        if (!_inventory.HasItem(recipe.productId, recipe.productCount))
        {
            result.failReason = CraftFailReason.NoOutputSpace;
            return result;
        }

        // 전부 통과
        result.canCraft = true;
        return result;
    }

    public CraftFailReason Craft(int recipeId)
    {
        var check = CanCraft(recipeId);
        if (!check.canCraft)
            return check.failReason;

        var recipe = check.recipe;

        foreach (var mat in recipe.materials)
        {
            _inventory.RemoveItem(mat.itemId, mat.count);
        }
        
        _inventory.AddItem(recipe.productId, recipe.productCount);
        return CraftFailReason.None;
    }
}
