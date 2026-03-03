using UnityEngine;

public class WaterPurificationFacilityInteractable : BuildingInteractableBase
{

    private ArcData arcData;

    private void Start()
    {
        arcData = BuildManager.I.ArcDB.TryGet(building.arcId, out var data) ? data : null;
    }

    public override void Interact(PlayerController player)
    {
        var inventory = player?.Inventory;
        if (inventory == null && player == null) return;
        if (!inventory.HasItem(4002001, 1))
        {
            ToastMessageUI.Instance.Show("정화시킬 물이 없습니다.");
            return;
        }

        inventory.TryRemoveItem(4002001, 1);
        inventory.AddItemFromWorld(4002002, 1); // LSY: 인벤토리 가득 찼을때 world에 떨어트리려면 AddItemFromWorld() 함수 사용

        SoundManager.I?.PlayWater();

        ToastMessageUI.Instance.Show(arcData != null ? arcData.interactText : "오염된 물을 정수했습니다");
    }
}
