using UnityEngine;

public class InventoryMessageService : MonoBehaviour
{
    [SerializeField] private ItemToastMessageUI itemToastUI;
    //[SerializeField] private MessageUI messageUI;

    public void ShowItemAcquired(ItemBase item, int count)
    {
        if (item == null || count <= 0) return;

        string text = count > 1
            ? $"{item.itemName} +{count}"
            : $"{item.itemName} 획득";

        //itemToastUI.Show(text, item.icon);
        itemToastUI.Show(text, item.icon);
    }

    public void ShowInventoryFull(int added, int requested)
    {
        if (added >= requested) return;
        itemToastUI.ShowWarning(
            $"인벤토리 부족! {added}/{requested}개만 획득"
        );
    }

    public void ShowWarning(string message)
    {
        itemToastUI.ShowWarning(message);
    }
}