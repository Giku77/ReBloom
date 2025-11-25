using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private int currentRecipeId;
    private CraftingManager crafting;
    private CraftRecipeDB recipeDb;

    [SerializeField] private InventoryItemData inventory;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI recipeDescText;
    [SerializeField] private TextMeshProUGUI recipeMaterialsText;
    [SerializeField] private TextMeshProUGUI recipeResultText;



    [SerializeField] private Transform slotParent;
    [SerializeField] private CraftingSlotUI slotPrefab;

    [SerializeField] private InputAction toggleCraftingUIAction;

    private void OnEnable()
    {
        Debug.Log("CraftingUI OnEnable");
        toggleCraftingUIAction.Enable();
        toggleCraftingUIAction.performed += OnToggleCraftingUI;
    }

    private void OnDisable()
    {
        toggleCraftingUIAction.performed -= OnToggleCraftingUI;
        toggleCraftingUIAction.Disable();
    }

    private void OnToggleCraftingUI(InputAction.CallbackContext context)
    {
        if (gameObject.activeSelf)
          Toggle();
    }

    private void Awake()
    {
        recipeDb = new CraftRecipeDB();
        recipeDb.LoadFromBG();
        crafting = new CraftingManager(recipeDb, inventory);
        craftButton.onClick.AddListener(OnClickCraftButton);
    }

    private void Start()
    {
        var recipes = recipeDb.GetAll();
        foreach (var recipe in recipes)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.Init(recipe.Value.recipeId, recipe.Value.productName, this);
        }
        Toggle();
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
                    setResultText("재료가 부족합니다.");
                    break;
                case CraftFailReason.NoOutputSpace:
                    setResultText("인벤토리에 공간이 부족합니다.");
                    break;
                // ...
            }
            return;
        }

        var reason = crafting.Craft(currentRecipeId);
        if (reason == CraftFailReason.None)
        {
            // 성공 UI 갱신
            setResultText("제작 성공!");
        }
    }

    public void setResultText(string result)
    {
        recipeResultText.text = result;
    }

    public void SelectRecipe(int recipeId)
    {
        currentRecipeId = recipeId;

        recipeDb.TryGet(recipeId, out var recipe);
        if (recipe == null)
            return;

        recipeNameText.text      = recipe.productName;  
        //recipeDescText.text      = recipe.Description;  
        recipeMaterialsText.text = BuildMaterialText(recipe); 

        recipeResultText.text = ""; 
    }

    private string BuildMaterialText(CraftRecipeData recipe)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("필요 재료 : ");
        foreach (var mat in recipe.materials)
        {
            var itemName = mat.name;
            sb.AppendLine($"{itemName} x{mat.count}");
        }
        return sb.ToString();
    }

    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
