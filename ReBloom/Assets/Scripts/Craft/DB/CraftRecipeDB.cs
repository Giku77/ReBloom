using System.Collections.Generic;
using BansheeGz.BGDatabase;

public class CraftRecipeDB
{
    private readonly Dictionary<int, CraftRecipeData> _recipes = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Item_Crafting");

        foreach (var e in meta.EntitiesToList())
        {
            var recipe = new CraftRecipeData
            {
                recipeId        = e.Get<int>("CraftRecipe_ID"),
                arcId           = e.Get<int>("Arc_ID"),
                productCategory = e.Get<int>("Product_Category"),
                productId       = e.Get<int>("Product_ID"),
                productName     = e.Get<string>("Product_Name"),
                productCount    = e.Get<int>("Product_Count"),

                needArc1Id      = e.Get<int>("Need_Ark1_ID"),
                needArc2Id      = e.Get<int>("Need_Ark2_ID"),
                needArc3Id      = e.Get<int>("Need_Ark3_ID"),
            };

            // 재료 세팅 (최대 3개)
            var materials = new List<CraftMaterialData>(3);

            // Material1
            {
                int id    = e.Get<int>("Material1_ID");
                string nm = e.Get<string>("M1Name");
                int cnt   = e.Get<int>("M1Count");

                if (id != 0 && cnt > 0)
                {
                    materials.Add(new CraftMaterialData
                    {
                        itemId = id,
                        name   = nm,
                        count  = cnt
                    });
                }
            }

            // Material2
            {
                int id    = e.Get<int>("Material2_ID");
                string nm = e.Get<string>("M2Name");
                int cnt   = e.Get<int>("M2Count");

                if (id != 0 && cnt > 0)
                {
                    materials.Add(new CraftMaterialData
                    {
                        itemId = id,
                        name   = nm,
                        count  = cnt
                    });
                }
            }

            // Material3
            {
                int id    = e.Get<int>("Material3_ID");
                string nm = e.Get<string>("M3Name");
                int cnt   = e.Get<int>("M3Count");

                if (id != 0 && cnt > 0)
                {
                    materials.Add(new CraftMaterialData
                    {
                        itemId = id,
                        name   = nm,
                        count  = cnt
                    });
                }
            }

            recipe.materials = materials.ToArray();

            _recipes[recipe.recipeId] = recipe;
        }
    }

    public bool TryGet(int recipeId, out CraftRecipeData data)
        => _recipes.TryGetValue(recipeId, out data);

    public Dictionary<int, CraftRecipeData> GetAll()
        => _recipes;

 
    public List<CraftRecipeData> GetByArcId(int arcId)
    {
        var list = new List<CraftRecipeData>();
        foreach (var kvp in _recipes)
        {
            if (kvp.Value.arcId == arcId)
                list.Add(kvp.Value);
        }
        return list;
    }
}
