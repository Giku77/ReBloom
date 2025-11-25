using UnityEngine;

public class CraftingInteractable : BuildingInteractableBase
{
    public override void Interact(PlayerController player)
    {
        // 크래프팅 UI 토글
        Debug.Log("크래프팅 인터랙트");
        player.OpenCraftingUI();
    }
}
