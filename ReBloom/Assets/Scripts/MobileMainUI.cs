using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Image sprintButtonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color sprintColor = Color.green;

    private bool isSprinting = false;

    protected override void OnShow()
    {
        base.OnShow();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.AddListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJump);

        if (interactButton != null)
            interactButton.onClick.AddListener(OnInteract);

        UpdateSprintButtonColor();
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.RemoveListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJump);

        if (interactButton != null)
            interactButton.onClick.RemoveListener(OnInteract);
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
        UpdateSprintButtonColor();
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

    private void UpdateSprintButtonColor()
    {
        if (sprintButtonImage != null)
        {
            sprintButtonImage.color = isSprinting ? sprintColor : normalColor;
        }
    }
}