using UnityEngine;

public class WaterSource : MonoBehaviour, IInteractable
{
    [Header ("Reference")]
    [SerializeField] private GameInventory inventoryItemData;

    private bool Available => inventoryItemData.HasItem(4102035, 1);
    private InteractionHighlight highlight;
    private string availableText = "물 채취 [E]";
    private string notAvailableText = "빈 통이 없으면 물을 채취할 수 없습니다.";

    public float HoldTime => 1.5f;

    private void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
    }

    private void Start()
    {
        if (highlight != null)
        {
            highlight.promptFormat = availableText;
        }
    }

    public bool CanInteract()
    {
        if (!Available)
        {
            ToastMessageUI.Instance.Show(notAvailableText);
            return false;
        }

        return true;

        //return Available;
    }

    public void Interact(PlayerController player)
    {
        if (player == null) return;
        if (inventoryItemData == null) return;

        inventoryItemData.RemoveItem(4102035, 1);
        inventoryItemData.AddItemFromWorld(4002001, 1, true);

        SoundManager.I?.PlayWater();
    }
}
