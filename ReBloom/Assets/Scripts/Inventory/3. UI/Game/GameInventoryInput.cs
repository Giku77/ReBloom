using UnityEngine;
using UnityEngine.InputSystem;

public class GameInventoryInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInventory gameInventory;
    [SerializeField] private QuickSlot quickSlot;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (gameInventory == null)
        {
            Debug.LogError("[GameInventoryInput] GameInventory가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions == null) return;

        inputActions.GameInventory.Enable();
        SubscribeInputActions();
    }

    private void OnDisable()
    {
        if (inputActions == null) return;

        UnsubscribeInputActions();
        inputActions.GameInventory.Disable();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }
    }

    #region Input Actions 이벤트 구독
    private void SubscribeInputActions()
    {
        if (inputActions == null) return;

        var gameInventoryMap = inputActions.GameInventory;
        gameInventoryMap.ToggleInventory.performed += OnToggleInventory;
        gameInventoryMap.AssignQuickSlot.performed += OnFillQuickSlots;
    }

    private void UnsubscribeInputActions()
    {
        if (inputActions == null) return;

        var gameInventoryMap = inputActions.GameInventory;
        gameInventoryMap.ToggleInventory.performed -= OnToggleInventory;
        gameInventoryMap.AssignQuickSlot.performed -= OnFillQuickSlots;
    }
    #endregion

    #region Input Callbacks
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    private void OnFillQuickSlots(InputAction.CallbackContext context)
    {
        quickSlot?.AutoFillQuickSlots();
    }
    #endregion

    #region 인벤토리 제어
    public void ToggleInventory()
    {
        gameInventory?.OpenInventory();
    }
    #endregion
}