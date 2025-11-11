using System.Collections.Generic;
using BansheeGz.BGDatabase;

public class ArcRecipeDB
{
    private Dictionary<int, ArcRecipe> _recipes = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Building_Crafting");
        foreach (var e in meta.EntitiesToList())
        {
            var r = new ArcRecipe();
            r.craftingId = e.Get<int>("ArCraftingId");
            r.arcId = e.Get<int>("productionresult");
            r.craftingType = e.Get<int>("CraftingType");

            var list = new List<(int,int)>();
            var ing1 = e.Get<int>("ArCraftingIng1");
            var amt1 = e.Get<int>("ArCraftingIng1amount");
            if (ing1 != 0) list.Add((ing1, amt1));

            var ing2 = e.Get<int>("ArCraftingIng2");
            var amt2 = e.Get<int>("ArCraftingIng2amount");
            if (ing2 != 0) list.Add((ing2, amt2));

            var ing3 = e.Get<int>("ArCraftingIng3");
            var amt3 = e.Get<int>("ArCraftingIng3amount");
            if (ing3 != 0) list.Add((ing3, amt3));

            r.materials = list;
            _recipes[r.arcId] = r;
        }
    }

    public bool TryGetRecipe(int arcId, out ArcRecipe recipe) => _recipes.TryGetValue(arcId, out recipe);
}
