using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingSlotUI : MonoBehaviour
{
    [SerializeField] private int recipeId;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotIcon;
    [SerializeField] private Sprite defualtIcon;

    private CraftingUI craftingUI;

    private void Awake()
    {
        craftingUI = FindFirstObjectByType<CraftingUI>();
        slotButton.onClick.AddListener(OnClickSlot);
    }

    public void Init(int id, int productId, string displayName, CraftingUI ui)
    {
        recipeId = id;
        craftingUI = ui;
        slotNameText.text = displayName;
        if (ItemDatabase.I.GetItem(productId).icon != null)
        {
            slotIcon.sprite = ItemDatabase.I.GetItem(productId).icon;
        }
        else
        {
            slotIcon.sprite = defualtIcon;
        }
    }

    private void OnClickSlot()
    {
        if (craftingUI == null) return;

        SoundManager.I?.PlayUIClick();
        craftingUI.SelectRecipe(recipeId);
    }

    public void Select()
    {
        if (craftingUI == null) return;

        craftingUI.SelectRecipe(recipeId);
    }
}
