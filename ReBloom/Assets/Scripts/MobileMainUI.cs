using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class MobileMainUI : UIBase
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractable playerInteractable;
    [SerializeField] private ScanController scanController;

    [Header("Joystick")]
    [SerializeField] private FixedJoystick movementJoystick;

    [Header("Buttons")]
    [SerializeField] private Button sprintToggleButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button buildButton;

    [Header("UI Feedback")]
    [SerializeField] private GameObject runImage;
    [SerializeField] private GameObject walkImage;

    private bool isSprinting = false;

    protected override void OnShow()
    {
        base.OnShow();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.AddListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpClicked);

        if (exploreButton != null)
            exploreButton.onClick.AddListener(OnExploreClicked);


        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryOpenClicked);


        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildClicked);
        //if (interactButton != null)
        //    interactButton.onClick.AddListener(OnInteract);

        UpdateRunImage();
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (sprintToggleButton != null)
            sprintToggleButton.onClick.RemoveListener(OnSprintToggle);

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpClicked);

        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(OnInventoryOpenClicked);

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
        UpdateRunImage();
    }

    private void OnJumpClicked()
    {
        if (playerController != null)
        {
            playerController.RequestJump();
        }
    }

    private void UpdateRunImage()
    {
        if (runImage == null || walkImage == null) return;

        if (isSprinting)
        {
            runImage.SetActive(false);
            walkImage.SetActive(true);
        }
        else
        { 
            walkImage.SetActive(false);
            runImage.SetActive(true);
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

    private void OnInventoryOpenClicked()
    {
        UIManager.Instance?.ShowUI(UIType.Inventory);
    }

    private void OnBuildClicked()
    {
        UIManager.Instance?.ShowUI(UIType.Building);
    }

    private void OnExploreClicked()
    {
        scanController?.TriggerScan();
    }
}