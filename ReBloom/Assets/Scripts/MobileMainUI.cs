using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class MobileMainUI : UIBase
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractable playerInteractable;

    [Header("Joystick")]
    [SerializeField] private FixedJoystick movementJoystick;

    [Header("Buttons")]
    [SerializeField] private Button sprintToggleButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button interactButton;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI sprintText;

    private string sprint = "뛰기";
    private string walking = "걷기";

    private bool isSprinting = false;

    protected override void OnShow()
    {
        base.OnShow();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.AddListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJump);

        //if (interactButton != null)
        //    interactButton.onClick.AddListener(OnInteract);

        UpdateSprintText();
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.RemoveListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJump);

        //if (interactButton != null)
        //    interactButton.onClick.RemoveListener(OnInteract);
    }

    private void Update()
    {
        if (playerController == null || movementJoystick == null) return;

        Vector2 input = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);

        playerController.SetMobileInput(input, isSprinting);
    }

    private void OnSprintToggle()
    {
        isSprinting = !isSprinting;
        UpdateSprintText();
    }

    private void OnJump()
    {
        if (playerController != null)
        {
            playerController.RequestJump();
        }
    }

    private void OnInteract()
    {
        if (playerInteractable != null)
        {
            playerInteractable.TriggerInteract();
        }
    }

    private void UpdateSprintText()
    {
        if (sprintText != null)
        {
            sprintText.text = isSprinting ? walking : sprint;
        }
    }

    public void OnInteractDown(BaseEventData data)
    {
        if (playerInteractable != null)
            playerInteractable.TriggerInteract();
    }

    public void OnInteractUp(BaseEventData data)
    {
        if (playerInteractable != null)
            playerInteractable.CancelMobileInteract();
    }
}