using UnityEngine;
using UnityEngine.InputSystem;

public class GameInventoryInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInventory gameInventory;

    private InputSystem_Actions inputActions;
    private bool isInventoryOpen = false;

    private void Awake()
    {
        if (gameInventory == null)
        {
            Debug.LogError("[GameInventoryInput] GameInventory가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        inputActions = new InputSystem_Actions();

        Debug.Log("[GameInventoryInput] 초기화 완료");
    }

    private void Start()
    {
        //HandleCursorState(false);
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

        Debug.Log("[GameInventoryInput] 입력 이벤트 구독 완료");
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
    /// <summary>
    /// I키 입력 - 인벤토리 열기/닫기
    /// </summary>
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    /// <summary>
    /// O키 입력 - 퀵슬롯 자동 채우기
    /// </summary>
    private void OnFillQuickSlots(InputAction.CallbackContext context)
    {
        FillQuickSlotsFromInventory();
    }
    #endregion

    #region 인벤토리 제어
    public void ToggleInventory()
    {
        if (gameInventory == null) return;

        isInventoryOpen = !isInventoryOpen;

        if (isInventoryOpen)
        {
            gameInventory.OpenInventory();
        }
        else
        {
            gameInventory.CloseInventory();
        }

        HandleCursorState(isInventoryOpen);
        Debug.Log($"[인벤토리] {(isInventoryOpen ? "열림" : "닫힘")}");
    }

    private void HandleCursorState(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
    #endregion

    #region 퀵슬롯 제어
    /// <summary>
    /// O키: 인벤토리 아이템을 순차적으로 퀵슬롯에 배치
    /// </summary>
    public void FillQuickSlotsFromInventory()
    {
        if (gameInventory == null)
        {
            Debug.LogWarning("[GameInventoryInput] GameInventory가 없습니다!");
            return;
        }

        int filledCount = gameInventory.AutoFillQuickSlots();

        Debug.Log($"[퀵슬롯] {filledCount}개 아이템 자동 배치 완료");
    }
    #endregion

    #region 디버그 명령어
    [ContextMenu("Debug/Toggle Inventory")]
    public void CMD_ToggleInventory()
    {
        ToggleInventory();
    }

    [ContextMenu("Debug/Fill Quick Slots")]
    public void CMD_FillQuickSlots()
    {
        FillQuickSlotsFromInventory();
    }
    #endregion
}