using UnityEngine;

public interface IInteractable
{
    public float HoldTime { get; }

    bool CanInteract();
    public void Interact(PlayerController player);
}
