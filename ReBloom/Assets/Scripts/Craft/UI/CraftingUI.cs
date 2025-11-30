using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftingUI : UIBase
{
    [SerializeField] private int currentRecipeId;
    private CraftingManager crafting;
    private CraftRecipeDB recipeDb;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Info References")]
    [SerializeField] private InventoryItemData inventory;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI recipeDescText;
    [SerializeField] private TextMeshProUGUI recipeMaterialsText;
    [SerializeField] private TextMeshProUGUI recipeResultText;


    [Header("Slot References")]
    [SerializeField] private Transform slotParent;
    [SerializeField] private CraftingSlotUI slotPrefab;


    [Header("Slider References")]
    [SerializeField] private Slider craftingCountSlider;
    [SerializeField] private TextMeshProUGUI craftingCountText;
    [SerializeField] private Button craftingCountAddButton;
    [SerializeField] private Button craftingCountSubButton;

    private int maxCraftable = 0;
    private int selectedAmount = 0;
    private GameObject player;

    protected override void Awake()
    {
        base.Awake();
        recipeDb = new CraftRecipeDB();
        recipeDb.LoadFromBG();
        crafting = new CraftingManager(recipeDb, inventory);
        craftButton.onClick.AddListener(OnClickCraftButton);
        if (craftingCountSlider != null)
            craftingCountSlider.onValueChanged.AddListener(OnSliderValueChanged);
        OnValidate();
        player = GameObject.FindWithTag("Player");
    }

    private void OnSliderValueChanged(float value)
    {
        selectedAmount = Mathf.RoundToInt(value);
        UpdateCraftCountText();
    }

    private void UpdateCraftCountText()
    {
        craftingCountText.text = $"{selectedAmount} / {maxCraftable}";
    }

    private void OnValidate()
    {
        if (craftingCountAddButton != null)
            craftingCountAddButton.onClick.RemoveAllListeners();
        if (craftingCountSubButton != null)
            craftingCountSubButton.onClick.RemoveAllListeners();

        if (craftingCountAddButton != null)
            craftingCountAddButton.onClick.AddListener(() =>
            {
                Debug.Log("Add Button Clicked");
                if (selectedAmount < maxCraftable)
                {
                    selectedAmount++;
                    if (craftingCountSlider != null)
                        craftingCountSlider.value = selectedAmount;
                    UpdateCraftCountText();
                }
            });

        if (craftingCountSubButton != null)
            craftingCountSubButton.onClick.AddListener(() =>
            {
                Debug.Log("Sub Button Clicked");
                if (selectedAmount > 0)
                {
                    selectedAmount--;
                    if (craftingCountSlider != null)
                        craftingCountSlider.value = selectedAmount;
                    UpdateCraftCountText();
                }
            });
    }

    private void Start()
    {
        var recipes = recipeDb.GetAll();
        CraftingSlotUI firstslot = null;
        foreach (var recipe in recipes)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            if (firstslot == null)
                firstslot = slot;
            slot.Init(recipe.Value.recipeId, recipe.Value.productName, this);
        }
        firstslot?.Select();
    }

    public void OnClickCraftButton()
    {
        if (selectedAmount <= 0)
        {
            setResultText("제작 수량을 선택해주세요.");
            return;
        }

        var check = crafting.CanCraft(currentRecipeId, selectedAmount);

        if (!check.canCraft)
        {
            switch (check.failReason)
            {
                case CraftFailReason.NotEnoughMaterials:
                    setResultText("재료가 부족합니다.");
                    // check.missingMaterials 써서 상세 메시지 가능
                    break;
                case CraftFailReason.NoOutputSpace:
                    setResultText("인벤토리에 공간이 부족합니다.");
                    break;
                // ...
            }
            return;
        }

        var reason = crafting.Craft(currentRecipeId, selectedAmount);
        if (reason == CraftFailReason.None || reason == CraftFailReason.NoOutputSpace)
        {
            if (itemSpawner != null && reason == CraftFailReason.NoOutputSpace)
            {
                recipeDb.TryGet(currentRecipeId, out var recipe);
                if (recipe != null)
                {
                    var pos = player.transform.position + Vector3.up * 0.5f;
                    var itemData = ItemDatabase.I.GetItem(recipe.productId);
                    itemSpawner.DropItemWithQuantity(itemData, pos, recipe.productCount * selectedAmount).Forget();
                }
            }
            setResultText($"제작 성공! x{selectedAmount}");

            maxCraftable = crafting.GetMaxCraftable(currentRecipeId);

            if (craftingCountSlider != null)
            {
                craftingCountSlider.maxValue = maxCraftable;
                selectedAmount = 0;
                craftingCountSlider.value = selectedAmount;
            }
            UpdateCraftCountText();
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

        recipeNameText.text = recipe.productName;  
        recipeMaterialsText.text = BuildMaterialText(recipe); 
        recipeResultText.text = string.Empty;

        maxCraftable = crafting.GetMaxCraftable(recipeId);

        if (craftingCountSlider != null)
        {
            craftingCountSlider.wholeNumbers = true;
            craftingCountSlider.minValue = 0;              
            craftingCountSlider.maxValue = maxCraftable;
        
            selectedAmount = 0;
            craftingCountSlider.value = selectedAmount;
        }

        UpdateCraftCountText();
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
        //gameObject.SetActive(!gameObject.activeSelf);
        //if (gameObject.activeSelf)
        //{
        //    Cursor.visible = true;
        //    Cursor.lockState = CursorLockMode.None;
        //    backgroundImage.gameObject.SetActive(true);
        //    Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = true;
        //}
        //else
        //{
        //    Cursor.visible = false;
        //    Cursor.lockState = CursorLockMode.Locked;
        //    backgroundImage.gameObject.SetActive(false);
        //    Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = false;
        //}
        UIManager.Instance?.ToggleUI(Type);
    }

    protected override void OnShow()
    {
        backgroundImage.gameObject.SetActive(true);
    }
    protected override void OnHide()
    {
        backgroundImage.gameObject.SetActive(false);
    }
}
