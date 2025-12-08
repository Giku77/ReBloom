using UnityEngine;

public class StorageInteractable : BuildingInteractableBase
{
    public override void Interact(PlayerController player)
    {
        //Debug.Log("창고 인터랙트");
        //player.OpenStorageUI();
        player.OpenStorage(this);
    }
}
