using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIInput : MonoBehaviour
{
    //[SerializeField] private InputAction InventoryAction;
    //[SerializeField] private InputAction CraftingAction;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private InputAction EscapeAction;
    [SerializeField] private InputAction DialogueAction;

    [SerializeField] private InputAction SkipCutScene;
    [SerializeField] private CutSceneManager cutSceneManager;
    [SerializeField] private float skipHoldDuration = 3f;
    [SerializeField] private GameObject skipHoldUI;
    [SerializeField] private Image skipHoldFill;

    private bool skipHeld;
    private float skipHoldTime;
    private bool skipTriggered;
    //[SerializeField] private InputAction BuildingAction;

    private void OnEnable()
    {
        EscapeAction.Enable();
        EscapeAction.performed += OnEsc;
        DialogueAction.Enable();
        DialogueAction.performed += dialogueUI.OnNextInput;
        SkipCutScene.Enable();
        SkipCutScene.started += OnSkipStarted;
        SkipCutScene.canceled += OnSkipCanceled;
    }
    private void OnDisable()
    {
        EscapeAction.performed -= OnEsc;
        EscapeAction.Disable();
        DialogueAction.performed -= dialogueUI.OnNextInput;
        DialogueAction.Disable();
        SkipCutScene.started -= OnSkipStarted;
        SkipCutScene.canceled -= OnSkipCanceled;
        SkipCutScene.Disable();
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        UIManager.Instance.OnEscPressed();
    }

    private void Update()
    {
        if (!skipHeld)
            return;

        skipHoldTime += Time.unscaledDeltaTime;

        if (skipHoldFill != null)
        {
            skipHoldFill.fillAmount = Mathf.Clamp01(skipHoldTime / skipHoldDuration);
        }

        if (!skipTriggered && skipHoldTime >= skipHoldDuration)
        {
            skipTriggered = true;
            skipHeld = false;

            if (cutSceneManager != null && cutSceneManager.IsPlaying)
            {
                cutSceneManager.SkipCutScene();
                if (skipHoldUI != null) skipHoldUI.SetActive(false);
                Debug.Log("[UIInput] CutScene Skip");
            }
        }
    }

    private void OnSkipStarted(InputAction.CallbackContext ctx)
    {
        skipHeld = true;
        skipHoldTime = 0f;
        skipTriggered = false;
    }

    private void OnSkipCanceled(InputAction.CallbackContext ctx)
    {
        skipHeld = false;

        if (!skipTriggered)
        {
            UIManager.Instance.OnEscPressed();
        }
        if (skipHoldFill != null)
        {
            skipHoldFill.fillAmount = 0f;
        }
    }
}
