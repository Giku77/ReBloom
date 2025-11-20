using UnityEngine;

public abstract class BuildingInteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected BuildingInstance building;
    protected ToastMessageUI toastMessageUI;

    public virtual float HoldTime => 0f;

    protected virtual void Awake()
    {
        if (building == null)
            building = GetComponent<BuildingInstance>();
        toastMessageUI = FindFirstObjectByType<ToastMessageUI>();
    }

    public abstract void Interact(PlayerController player);

    public bool CanInteract()
    {
        return true;
    }
}
