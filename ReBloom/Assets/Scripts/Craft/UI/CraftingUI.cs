using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private int currentRecipeId;
    private CraftingManager crafting;

    [SerializeField] private InventoryItemData inventory;
    [SerializeField] private Button craftButton;

    private void Awake()
    {
        var craftDb = new CraftRecipeDB();
        craftDb.LoadFromBG();
        crafting = new CraftingManager(craftDb, inventory);
        craftButton.onClick.AddListener(OnClickCraftButton);
    }

    public void OnClickCraftButton()
    {
        Debug.Log($"제작 시도: 레시피 ID {currentRecipeId}");
        var check = crafting.CanCraft(currentRecipeId);

        if (!check.canCraft)
        {
            switch (check.failReason)
            {
                case CraftFailReason.NotEnoughMaterials:
                    // check.missingMaterials 이용해서 "철 파편 x2 부족" 이런 메시지 가능
                    Debug.Log("재료가 부족합니다:");
                    break;
                case CraftFailReason.NoOutputSpace:
                    Debug.Log("인벤토리에 공간이 부족합니다.");
                    break;
                // ...
            }
            return;
        }

        var reason = crafting.Craft(currentRecipeId);
        if (reason == CraftFailReason.None)
        {
            // 성공 UI 갱신
            Debug.Log("제작 성공!");
        }
    }
}
