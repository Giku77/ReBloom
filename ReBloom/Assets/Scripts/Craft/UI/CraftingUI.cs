using UnityEngine;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private int currentRecipeId;
    public CraftingManager crafting;

    private void Awake()
    {
        var inventory = FindFirstObjectByType<GameInventory>();
        var craftDb = new CraftRecipeDB();
        craftDb.LoadFromBG();
        crafting = new CraftingManager(craftDb, inventory);
    }

    public void OnClickCraftButton()
    {
        var check = crafting.CanCraft(currentRecipeId);

        if (!check.canCraft)
        {
            switch (check.failReason)
            {
                case CraftFailReason.NotEnoughMaterials:
                    // check.missingMaterials 이용해서 "철 파편 x2 부족" 이런 메시지 가능
                    break;
                case CraftFailReason.NoOutputSpace:
                    // "인벤토리에 공간이 부족합니다"
                    break;
                // ...
            }
            return;
        }

        var reason = crafting.Craft(currentRecipeId);
        if (reason == CraftFailReason.None)
        {
            // 성공 UI 갱신
        }
    }
}
