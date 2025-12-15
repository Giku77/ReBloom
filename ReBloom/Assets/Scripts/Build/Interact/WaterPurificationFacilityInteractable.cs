using UnityEngine;

public class WaterPurificationFacilityInteractable : BuildingInteractableBase
{
    [Header ("Reference")]
    [SerializeField] private GameInventory inventory;

    public override void Interact(PlayerController player)
    {
        if (inventory == null && player == null) return;
        if (!inventory.HasItem(4002001, 1))
        {
            ToastMessageUI.Instance.Show("정화시킬 물이 없습니다.");
            return;
        }

        inventory.RemoveItem(4002001, 1);
        inventory.TryAddItemFromWorld(4002002, 1); // LSY: 인벤토리 가득 찼을때 world에 떨어트리려면 AddItemFromWorld() 함수 사용

        ToastMessageUI.Instance.Show("오염된 물을 정화했습니다.");

    }
}
