public class CraftMaterialData
{
    public int itemId;      
    public string name;    
    public int count;     
}

public class CraftRecipeData
{
    public int recipeId;        
    public int arcId;           // Arc_ID (어느 건축물/설비에서 만드는지)
    public int productCategory; 
    public int productId;      
    public string productName; 
    public int productCount;    

    // 최대 3개 재료 지원
    public CraftMaterialData[] materials;

    public int needArc1Id;      // Need_Ark1_ID
    public int needArc2Id;      // Need_Ark2_ID
    public int needArc3Id;      // Need_Ark3_ID
}
