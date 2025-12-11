using UnityEngine;

public class WaterPurificationFacilityInteractable : BuildingInteractableBase
{
    [Header ("Reference")]
    [SerializeField] private InventoryItemData inventoryItemData;

    public override void Interact(PlayerController player)
    {
        if (inventoryItemData == null && player == null) return;
        if (!inventoryItemData.HasItem(4002001, 1))
        {
            ToastMessageUI.Instance.Show("정화시킬 물이 없습니다.");
            return;
        }

        inventoryItemData.RemoveItem(4002001, 1);
        inventoryItemData.AddItem(4002002, 1);

        ToastMessageUI.Instance.Show("오염된 물을 정화했습니다.");

    }
}
