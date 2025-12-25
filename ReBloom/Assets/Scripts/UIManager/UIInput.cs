using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIInput : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private InputAction EscapeAction;
    [SerializeField] private InputAction DialogueAction;

    [Header("Skip CutScene")]
    [SerializeField] private InputAction SkipCutScene;   // PC용(키보드/패드)
    [SerializeField] private CutSceneManager cutSceneManager;
    [SerializeField] private float skipHoldDuration = 3f;
    [SerializeField] private GameObject skipHoldUI;
    [SerializeField] private Image skipHoldFill;

    private bool skipHeld;
    private float skipHoldTime;
    private bool skipTriggered;

    private void OnEnable()
    {
        EscapeAction.Enable();
        EscapeAction.performed += OnEsc;

        DialogueAction.Enable();
        DialogueAction.performed += dialogueUI.OnNextInput;

#if !(UNITY_ANDROID || UNITY_IOS)
        SkipCutScene.Enable();
        SkipCutScene.started += OnSkipStarted;
        SkipCutScene.canceled += OnSkipCanceled;
#endif
    }

    private void OnDisable()
    {
        EscapeAction.performed -= OnEsc;
        EscapeAction.Disable();

        DialogueAction.performed -= dialogueUI.OnNextInput;
        DialogueAction.Disable();

#if !(UNITY_ANDROID || UNITY_IOS)
        SkipCutScene.started -= OnSkipStarted;
        SkipCutScene.canceled -= OnSkipCanceled;
        SkipCutScene.Disable();
#endif
    }

    private bool WasPointerPressedThisFrame()
    {
#if UNITY_ANDROID || UNITY_IOS
        return Touchscreen.current != null &&
               Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
#else
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#endif
    }

    private bool IsPointerPressed()
    {
#if UNITY_ANDROID || UNITY_IOS
        return Touchscreen.current != null &&
               Touchscreen.current.primaryTouch.press.isPressed;
#else
        return false;
#endif
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        UIManager.Instance.OnEscPressed();
    }

    private void Update()
    {
        if (dialogueUI != null && dialogueUI.IsOpen)
        {
            if (WasPointerPressedThisFrame())
                dialogueUI.RequestNext();
        }

#if UNITY_ANDROID || UNITY_IOS

        bool pressed = IsPointerPressed();

        if (pressed)
        {
            if (!skipHeld)
            {
                skipHeld = true;
                skipHoldTime = 0f;
                skipTriggered = false;

                //if (skipHoldUI != null) skipHoldUI.SetActive(true);
                if (skipHoldFill != null) skipHoldFill.fillAmount = 0f;
            }
        }
        else
        {
            if (skipHeld)
            {
                skipHeld = false;

                if (skipHoldFill != null) skipHoldFill.fillAmount = 0f;
                //if (skipHoldUI != null) skipHoldUI.SetActive(false);
            }
        }
#endif

        if (!skipHeld)
            return;

        skipHoldTime += Time.unscaledDeltaTime;

        if (skipHoldFill != null)
            skipHoldFill.fillAmount = Mathf.Clamp01(skipHoldTime / skipHoldDuration);

        if (!skipTriggered && skipHoldTime >= skipHoldDuration)
        {
            skipTriggered = true;
            skipHeld = false;

            if (cutSceneManager != null && cutSceneManager.IsPlaying)
            {
                cutSceneManager.SkipCutScene();
                if (skipHoldUI != null) skipHoldUI.SetActive(false);
            }
        }
    }

    private void OnSkipStarted(InputAction.CallbackContext ctx)
    {
        skipHeld = true;
        skipHoldTime = 0f;
        skipTriggered = false;

        //if (skipHoldUI != null) skipHoldUI.SetActive(true);
        if (skipHoldFill != null) skipHoldFill.fillAmount = 0f;
    }

    private void OnSkipCanceled(InputAction.CallbackContext ctx)
    {
        skipHeld = false;

        if (skipHoldFill != null) skipHoldFill.fillAmount = 0f;
        //if (skipHoldUI != null) skipHoldUI.SetActive(false);
    }
}
