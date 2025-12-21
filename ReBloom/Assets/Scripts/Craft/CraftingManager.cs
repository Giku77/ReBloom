using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UIElements;

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
    private readonly GameInventory _inventory;

    public CraftingManager(CraftRecipeDB recipeDb, GameInventory inventory)
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
        return CanCraft(recipeId, 1);
    }

    public CraftCheckResult CanCraft(int recipeId, int amount)
    {
        var result = new CraftCheckResult
        {
            canCraft = false,
            failReason = CraftFailReason.None,
            missingMaterials = new Dictionary<int, int>()
        };

        if (amount <= 0)
        {
            result.failReason = CraftFailReason.None; 
            return result;
        }

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
            int need = mat.count * amount;  

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

        //if (_inventory.Items.Count >= _inventory.SlotCount)
        //{
        //    result.failReason = CraftFailReason.NoOutputSpace;
        //    return result;
        //}

        result.canCraft = true;
        return result;
    }

    public CraftFailReason Craft(int recipeId)
    {
        return Craft(recipeId, 1).reason;
    }

    public struct CraftResult
    {
        public CraftFailReason reason;
        public int overflowCount;
    }

    public CraftResult Craft(int recipeId, int amount)
    {
        var check = CanCraft(recipeId, amount);
        if (!check.canCraft)
            return new CraftResult { reason = check.failReason };

        var recipe = check.recipe;

        foreach (var mat in recipe.materials)
        {
            _inventory.RemoveItem(mat.itemId, mat.count * amount);
        }

        //if (!_inventory.AddItem(recipe.productId, recipe.productCount * amount))
        //{
        //    return CraftFailReason.NoOutputSpace;
        //}

        int totalProductCount = recipe.productCount * amount;
        var overflow = _inventory.AddItemFromWorld(recipe.productId, totalProductCount, true);

        SoundManager.I?.PlayCrafting();

        return new CraftResult
        {
            reason = overflow > 0 ? CraftFailReason.NoOutputSpace : CraftFailReason.None,
            overflowCount = overflow
        };
    }

    public int GetMaxCraftable(int recipeId)
    {
        if (!_recipeDb.TryGet(recipeId, out var recipe))
            return 0;

        int maxByMat = int.MaxValue;

        foreach (var mat in recipe.materials)
        {
            int have = _inventory.GetItemCount(mat.itemId);
            if (mat.count <= 0) 
                continue;

            int canByThisMat = have / mat.count;  
            if (canByThisMat < maxByMat)
                maxByMat = canByThisMat;
        }

        if (maxByMat == int.MaxValue)
            maxByMat = 0;

        // TODO: 인벤토리 공간도 고려하려면 여기에서 한 번 더 clamp
        // int maxByInventory = _inventory.GetMaxAddableCount(recipe.productId, recipe.productCount);
        // maxByMat = System.Math.Min(maxByMat, maxByInventory);

        return maxByMat;
    }
    
}
