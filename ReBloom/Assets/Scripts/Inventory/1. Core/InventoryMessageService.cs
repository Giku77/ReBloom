using UnityEngine;

public class InventoryMessageService : MonoBehaviour
{
    [SerializeField] private ItemToastUI toastUI;
    [SerializeField] private MessageUI messageUI;
    public void ShowItemAcquired(ItemBase item, int count)
    {
        toastUI.Show(item, count);
    } 

    public void ShowWarning(string message)
    {
        messageUI.Show(message, Color.yellow);
    }
}
