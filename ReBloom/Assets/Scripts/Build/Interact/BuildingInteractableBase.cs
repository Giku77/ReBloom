using UnityEngine;

public abstract class BuildingInteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected BuildingInstance building;

    public virtual float HoldTime => 0f;

    protected virtual void Awake()
    {
        if (building == null)
            building = GetComponent<BuildingInstance>();
    }

    public abstract void Interact(PlayerController player);

    public bool CanInteract()
    {
        return true;
    }
}
