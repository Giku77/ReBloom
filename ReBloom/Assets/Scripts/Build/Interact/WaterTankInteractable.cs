using UnityEngine;

public class WaterTankInteractable : BuildingInteractableBase
{
    public override void Interact(PlayerController player)
    {
        Debug.Log("물탱크 인터랙트");
        player.OpenWaterTankUI();
    }
}