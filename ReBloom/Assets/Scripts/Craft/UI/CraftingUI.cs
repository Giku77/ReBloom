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
    [SerializeField] private GameInventory inventory;
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
        RefreshMaterialText();
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

    private CraftingSlotUI firstslot;

    private void Start()
    {
        var recipes = recipeDb.GetAll();
        foreach (var recipe in recipes)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            if (firstslot == null)
                firstslot = slot;
            slot.Init(recipe.Value.recipeId, recipe.Value.productName, this);
        }
        firstslot?.Select();
    }

    private void OnEnable()
    {
        firstslot?.Select();
    }

    public void OnClickCraftButton()
    {
        if (selectedAmount <= 0 && maxCraftable > 0)
        {
            setResultText("제작 수량을 선택해주세요.");
            return;
        }

        if (selectedAmount <= 0)
        {
            setResultText("재료가 부족합니다.");
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

        var result = crafting.Craft(currentRecipeId, selectedAmount);

        if (result.reason == CraftFailReason.None || result.reason == CraftFailReason.NoOutputSpace)
        {
            if (result.overflowCount > 0)
            {
                recipeDb.TryGet(currentRecipeId, out var recipe);
                var pos = player.transform.position + Vector3.up * 0.5f;
                SoundManager.I?.PlayCrafting();
                itemSpawner.DropItemWithQuantity(ItemDatabase.I.GetItem(recipe.productId), pos, result.overflowCount).Forget();
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
            RefreshMaterialText();
        }
    }

    public void setResultText(string result)
    {
        recipeResultText.text = result;
    }

    private void RefreshMaterialText()
    {
        if (!recipeDb.TryGet(currentRecipeId, out var recipe))
            return;

        int amount = Mathf.Max(selectedAmount, 1); // 0이면 그냥 1개 기준으로 보여주게

        var check = crafting.CanCraft(currentRecipeId, amount);
        recipeMaterialsText.text = BuildMaterialText(recipe, check, amount);
    }


    public void SelectRecipe(int recipeId)
    {
        currentRecipeId = recipeId;

        recipeDb.TryGet(recipeId, out var recipe);
        if (recipe == null)
            return;

        recipeNameText.text = recipe.productName;  
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
        RefreshMaterialText();
    }

    private string BuildMaterialText(
        CraftRecipeData recipe,
        CraftCheckResult check,
        int amount)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (var mat in recipe.materials)
        {
            int need = mat.count * amount;

            check.missingMaterials?.TryGetValue(mat.itemId, out int missing);

            int owned = inventory.GetItemCount(mat.itemId);
            bool isLack = owned < need;
            int lack = System.Math.Max(0, need - owned);

            //if (isLack) sb.Append("<color=#ff8080>");   // 빨간색 계열
            //else        sb.Append("<color=#80ff80>");   // 초록색 계열

            //sb.Append($"{mat.name} (보유: {owned} / <color=#A0A0A0>소모: {need}");

            // if (isLack)
            //     sb.Append($", 부족: {lack}");
            if (isLack) sb.Append("<color=#ff8080>");
            sb.Append($"{mat.name} : {owned} / {need}");
            sb.Append("</color>\n");
            // sb.Append($"보유: {owned}</color>");
            // sb.Append(" / ");
            // sb.Append("<color=#a0a0a0>");
            // sb.Append($"소모: {need}");
            // sb.Append("</color>");
            // sb.Append(")\n");
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
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
          return;
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
