using UnityEngine;

public interface IInteractable
{
    public float HoldTime { get; }

    public void Interact(PlayerController player);
}
