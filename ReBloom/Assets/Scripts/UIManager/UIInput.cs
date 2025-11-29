using UnityEngine;
using UnityEngine.InputSystem;

public class UIInput : MonoBehaviour
{
    //[SerializeField] private InputAction InventoryAction;
    //[SerializeField] private InputAction CraftingAction;
    [SerializeField] private InputAction EscapeAction;
    //[SerializeField] private InputAction BuildingAction;

    private void OnEnable()
    {
        EscapeAction.Enable();
        EscapeAction.performed += OnEsc;
    }
    private void OnDisable()
    {
        EscapeAction.performed -= OnEsc;
        EscapeAction.Disable();
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        UIManager.Instance.OnEscPressed();
    }
}
