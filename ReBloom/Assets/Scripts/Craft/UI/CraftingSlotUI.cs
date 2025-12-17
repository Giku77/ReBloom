using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingSlotUI : MonoBehaviour
{
    [SerializeField] private int recipeId;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotIcon;

    private CraftingUI craftingUI;

    private void Awake()
    {
        craftingUI = FindFirstObjectByType<CraftingUI>();
        slotButton.onClick.AddListener(OnClickSlot);
    }

    public void Init(int id, string displayName, CraftingUI ui)
    {
        recipeId = id;
        craftingUI = ui;
        slotNameText.text = displayName;
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
