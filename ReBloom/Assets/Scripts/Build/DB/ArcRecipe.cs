using System.Collections.Generic;

public class ArcRecipe
{
    public int craftingId;
    public int arcId;               
    public int craftingType;       
    public List<(int itemId, int amount)> materials;
}
