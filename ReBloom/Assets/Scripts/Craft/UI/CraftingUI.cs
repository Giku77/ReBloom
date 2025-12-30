using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftingUI : UIBase
{
    [Header("Category Tab References")]
    [SerializeField] private Button tabAll;      // 전체 탭
    [SerializeField] private Button tabCategory1; // 도구/장비
    [SerializeField] private Button tabCategory2; // 소비
    [SerializeField] private Button tabCategory3; // 재료

    [SerializeField] private Color tabActiveColor = Color.white;
    [SerializeField] private Color tabInactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private int currentCategory = 0; // 0 = 전체
    private List<CraftingSlotUI> spawnedSlots = new List<CraftingSlotUI>();

    [SerializeField] private int currentRecipeId;
    private CraftingManager crafting;
    private CraftRecipeDB recipeDb;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Info References")]
    [SerializeField] private GameInventory inventory;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI recipeDescText;
    //[SerializeField] private TextMeshProUGUI recipeMaterialsText;
    [SerializeField] private TextMeshProUGUI recipeResultText;
    [SerializeField] private Image tierBorder;

    [Tooltip("Ingredient UI References")]
    [System.Serializable]
    public class IngredientSlot
    {
        public GameObject container;      // 전체 컨테이너 (SetActive용)
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI quantityText;
    }

    [SerializeField] private Image recipeIcon;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private List<IngredientSlot> ingredientSlots;
    [SerializeField] private Color lackColor = new Color(1f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    [SerializeField] private Color deafultIconColor = new Color(1, 1, 1, 0.7f);

    [SerializeField] private TextMeshProUGUI buildingRequireText;
    // 재료아이콘 넣을때는 (1,1,1,1) 사용 / deafultIcon은 반투명 설정되어 있음

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

        if (tabAll != null)
            tabAll.onClick.AddListener(() => OnTabClicked(0));
        if (tabCategory1 != null)
            tabCategory1.onClick.AddListener(() => OnTabClicked(1));
        if (tabCategory2 != null)
            tabCategory2.onClick.AddListener(() => OnTabClicked(2));
        if (tabCategory3 != null)
            tabCategory3.onClick.AddListener(() => OnTabClicked(3));
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
        //var recipes = recipeDb.GetAll();
        //foreach (var recipe in recipes)
        //{
        //    var slot = Instantiate(slotPrefab, slotParent);
        //    if (firstslot == null)
        //        firstslot = slot;
        //    slot.Init(recipe.Value.recipeId, recipe.Value.productId, recipe.Value.productName, this);
        //}
        //firstslot?.Select();

        OnTabClicked(0);
    }

    private void OnEnable()
    {
        firstslot?.Select();
    }

    public void OnClickCraftButton()
    {
        if (selectedAmount <= 0 && maxCraftable > 0)
        {
            SoundManager.I?.PlayError();
            setResultText("제작 수량을 선택해주세요.");
            return;
        }

        if (selectedAmount <= 0)
        {
            SoundManager.I?.PlayError();
            setResultText("재료가 부족합니다.");
            return;
        }

        var check = crafting.CanCraft(currentRecipeId, selectedAmount);

        if (!check.canCraft)
        {
            switch (check.failReason)
            {
                case CraftFailReason.NotEnoughMaterials:
                    SoundManager.I?.PlayError();
                    setResultText("재료가 부족합니다.");
                    // check.missingMaterials 써서 상세 메시지 가능
                    break;
                case CraftFailReason.NoOutputSpace:
                    SoundManager.I?.PlayError();
                    setResultText("인벤토리에 공간이 부족합니다.");
                    break;
                // ...
            }
            return;
        }

        var result = crafting.Craft(currentRecipeId, selectedAmount);

        if (result.reason == CraftFailReason.None || result.reason == CraftFailReason.NoOutputSpace)
        {
            SoundManager.I?.PlayCrafting();
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
        BuildMaterialText(recipe, check, amount);
    }


    public void SelectRecipe(int recipeId)
    {
        currentRecipeId = recipeId;

        recipeDb.TryGet(recipeId, out var recipe);
        if (recipe == null)
            return;

        recipeNameText.text = recipe.productName;
        var item = ItemDatabase.I.GetItem(recipe.productId);

        if (item.icon != null)
        {
            recipeIcon.sprite = item.icon;
            recipeDescText.text = item.description != null ? item.description : "설명이 없습니다.";
            tierText.text = "Tier " + item.tier.ToString();
            tierBorder.color = GameInventoryToolTip.GetTierColor(item.tier);
            tierText.gameObject.SetActive(item.tier != 0);
        }
        else
        {
            recipeIcon.sprite = defaultIcon;
        }
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

    private void BuildMaterialText(
        CraftRecipeData recipe,
        CraftCheckResult check,
        int amount)
    {
        foreach (var slot in ingredientSlots)
        {
            slot.container.SetActive(false);
        }

        // 2) 재료 수만큼만 보이기
        for (int i = 0; i < recipe.materials.Length; i++)
        {
            if (i >= ingredientSlots.Count) break; // 슬롯 부족하면 스킵

            var mat = recipe.materials[i];
            var slot = ingredientSlots[i];

            int need = mat.count * amount;
            int owned = inventory.GetItemCount(mat.itemId);
            bool isLack = owned < need;

            // 슬롯 활성화
            slot.container.SetActive(true);

            // 아이콘
            var itemData = ItemDatabase.I.GetItem(mat.itemId);
            slot.icon.sprite = itemData?.icon ?? defaultIcon;

            // 이름
            slot.nameText.text = mat.name;
            slot.nameText.color = isLack ? lackColor : normalColor;

            // 수량
            slot.quantityText.text = $"{owned} / {need}";
            slot.quantityText.color = isLack ? lackColor : normalColor;
        }
    }

    private void OnTabClicked(int category)
    {
        currentCategory = category;
        UpdateTabVisuals();
        RefreshSlots();
    }

    private void UpdateTabVisuals()
    {
        // 선택된 탭만 활성 색상으로
        SetTabColor(tabAll, currentCategory == 0);
        SetTabColor(tabCategory1, currentCategory == 1);
        SetTabColor(tabCategory2, currentCategory == 2);
        SetTabColor(tabCategory3, currentCategory == 3);
    }

    private void SetTabColor(Button tab, bool isActive)
    {
        if (tab == null) return;

        var colors = tab.colors;
        colors.normalColor = isActive ? tabActiveColor : tabInactiveColor;
        tab.colors = colors;

        // 또는 Image 컴포넌트 직접 변경
        var img = tab.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? tabActiveColor : tabInactiveColor;
    }

    private void RefreshSlots()
    {
        // 1. 기존 슬롯 전부 삭제
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        spawnedSlots.Clear();
        firstslot = null;

        // 2. 필터링된 레시피로 새 슬롯 생성
        var recipes = recipeDb.GetByCategory(currentCategory);

        foreach (var recipe in recipes)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            spawnedSlots.Add(slot);

            if (firstslot == null)
                firstslot = slot;

            slot.Init(recipe.recipeId, recipe.productId, recipe.productName, this);
        }

        // 3. 첫 번째 슬롯 자동 선택
        firstslot?.Select();
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
        SoundManager.I?.PlayOpenCraftingTable();
    }
    protected override void OnHide()
    {
        backgroundImage.gameObject.SetActive(false);
        SoundManager.I?.PlayCloseCraftingTable();

    }
}
