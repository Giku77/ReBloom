using UnityEngine;
using UnityEngine.InputSystem;

public class UIInput : MonoBehaviour
{
    //[SerializeField] private InputAction InventoryAction;
    //[SerializeField] private InputAction CraftingAction;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private InputAction EscapeAction;
    [SerializeField] private InputAction DialogueAction;
    //[SerializeField] private InputAction BuildingAction;

    private void OnEnable()
    {
        EscapeAction.Enable();
        EscapeAction.performed += OnEsc;
        DialogueAction.Enable();
        DialogueAction.performed += dialogueUI.OnNextInput;
    }
    private void OnDisable()
    {
        EscapeAction.performed -= OnEsc;
        EscapeAction.Disable();
        DialogueAction.performed -= dialogueUI.OnNextInput;
        DialogueAction.Disable();
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        UIManager.Instance.OnEscPressed();
    }
}
