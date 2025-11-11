using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 디버그 인벤토리 입력 처리
/// 자체 InputActions 인스턴스 생성 및 관리
/// </summary>
public class DebugInventoryInput : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private bool enableInReleaseBuild = false;

    [Header("Ref")]
    [SerializeField] private DebugInventoryUI debugUI;

    private InputSystem_Actions inputActions;
    private bool isUIOpen = false;

    private void Awake()
    {
        // 릴리즈 빌드에서 비활성화
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (!enableInReleaseBuild)
        {
            enabled = false;
            return;
        }
#endif

        // DebugInventoryUI 유효성 검사
        if (debugUI == null)
        {
            Debug.LogError("[DebugInventoryInput] DebugInventoryUI가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        // InputActions 인스턴스 생성 (중요!)
        inputActions = new InputSystem_Actions();

        Debug.Log("[DebugInventoryInput] 초기화 완료");
    }

    private void Start()
    {
        HandleCursorState(false);
    }

    private void OnEnable()
    {
        if (inputActions == null) return;

        // Action Map 활성화
        inputActions.DebugInventory.Enable();

        // 이벤트 구독
        SubscribeInputActions();
    }

    private void OnDisable()
    {
        if (inputActions == null) return;

        // 이벤트 구독 해제
        UnsubscribeInputActions();

        // Action Map 비활성화
        inputActions.DebugInventory.Disable();
    }

    private void OnDestroy()
    {
        // InputActions 정리
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

        var debugMap = inputActions.DebugInventory;

        debugMap.Toggle.performed += OnToggle;
        debugMap.Close.performed += OnClose;
        debugMap.Refresh.performed += OnRefresh;
        debugMap.Search.performed += OnSearch;
        debugMap.ResetFilters.performed += OnResetFilters;
        debugMap.SwitchTable1.performed += OnSwitchTable1;
        debugMap.SwitchTable2.performed += OnSwitchTable2;
        debugMap.SwitchTable3.performed += OnSwitchTable3;
        debugMap.SwitchTable4.performed += OnSwitchTable4;

        Debug.Log("[DebugInventoryInput] 입력 이벤트 구독 완료");
    }

    private void UnsubscribeInputActions()
    {
        if (inputActions == null) return;

        var debugMap = inputActions.DebugInventory;

        debugMap.Toggle.performed -= OnToggle;
        debugMap.Close.performed -= OnClose;
        debugMap.Refresh.performed -= OnRefresh;
        debugMap.Search.performed -= OnSearch;
        debugMap.ResetFilters.performed -= OnResetFilters;
        debugMap.SwitchTable1.performed -= OnSwitchTable1;
        debugMap.SwitchTable2.performed -= OnSwitchTable2;
        debugMap.SwitchTable3.performed -= OnSwitchTable3;
        debugMap.SwitchTable4.performed -= OnSwitchTable4;
    }
    #endregion

    #region Input Callbacks
    private void OnToggle(InputAction.CallbackContext context)
    {
        ToggleDebugUI();
    }

    private void OnClose(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            CloseDebugUI();
        }
    }

    private void OnRefresh(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            RefreshItemList();
        }
    }

    private void OnSearch(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            FocusSearchField();
        }
    }

    private void OnResetFilters(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            ResetFilters();
        }
    }

    private void OnSwitchTable1(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            SwitchToTable(ItemTableType.Consumable);
        }
    }

    private void OnSwitchTable2(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            SwitchToTable(ItemTableType.Protective);
        }
    }

    private void OnSwitchTable3(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            SwitchToTable(ItemTableType.Tool);
        }
    }

    private void OnSwitchTable4(InputAction.CallbackContext context)
    {
        if (isUIOpen)
        {
            SwitchToTable(ItemTableType.Misc);
        }
    }
    #endregion

    #region UI 제어
    public void ToggleDebugUI()
    {
        if (debugUI == null)
        {
            Debug.LogError("[DebugInventoryInput] DebugInventoryUI가 할당되지 않았습니다!");
            return;
        }

        isUIOpen = !isUIOpen;

        if (isUIOpen)
        {
            debugUI.OpenDebugInventory();
        }
        else
        {
            debugUI.CloseDebugInventory();
        }

        HandleCursorState(isUIOpen);
        Debug.Log($"[디버그 인벤토리] {(isUIOpen ? "열림" : "닫힘")}");
    }

    public void OpenDebugUI()
    {
        if (debugUI == null) return;

        isUIOpen = true;
        debugUI.OpenDebugInventory();
        HandleCursorState(true);

        Debug.Log("[디버그 인벤토리] 열림");
    }

    public void CloseDebugUI()
    {
        if (debugUI == null) return;

        isUIOpen = false;
        debugUI.CloseDebugInventory();
        HandleCursorState(false);

        Debug.Log("[디버그 인벤토리] 닫힘");
    }

    private void HandleCursorState(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
    #endregion

    #region 디버그 커맨드 구현
    private void RefreshItemList()
    {
        if (debugUI != null)
        {
            debugUI.RefreshItemList();
            Debug.Log("[디버그 인벤토리] 새로고침됨");
        }
    }

    private void FocusSearchField()
    {
        // TODO: DebugInventoryUI에 FocusSearch() 메서드 추가 필요
        Debug.Log("[디버그 인벤토리] 검색창 포커스 (미구현)");
    }

    private void SwitchToTable(ItemTableType tableType)
    {
        if (debugUI == null) return;

        // 버튼 클릭으로 테이블 전환 시뮬레이션
        switch (tableType)
        {
            case ItemTableType.Consumable:
                debugUI.btnConsumable.onClick.Invoke();
                break;
            case ItemTableType.Protective:
                debugUI.btnProtective.onClick.Invoke();
                break;
            case ItemTableType.Tool:
                debugUI.btnTool.onClick.Invoke();
                break;
            case ItemTableType.Misc:
                debugUI.btnMisc.onClick.Invoke();
                break;
        }

        Debug.Log($"[디버그 인벤토리] {tableType} 테이블로 전환");
    }

    private void ResetFilters()
    {
        if (debugUI != null)
        {
            // TODO: 필터 리셋 로직 구현
            debugUI.RefreshItemList();
            Debug.Log("[디버그 인벤토리] 필터 리셋됨");
        }
    }
    #endregion

    #region 콘솔 명령어
    [ContextMenu("Debug/Open Inventory")]
    public void CMD_OpenInventory()
    {
        OpenDebugUI();
    }

    [ContextMenu("Debug/Close Inventory")]
    public void CMD_CloseInventory()
    {
        CloseDebugUI();
    }

    [ContextMenu("Debug/Toggle Inventory")]
    public void CMD_ToggleInventory()
    {
        ToggleDebugUI();
    }

    [ContextMenu("Debug/Print Hotkeys")]
    public void CMD_PrintHotkeys()
    {
        Debug.Log("=== 디버그 인벤토리 단축키 ===");
        Debug.Log("열기/닫기: F1");
        Debug.Log("닫기: ESC");
        Debug.Log("새로고침: F5");
        Debug.Log("검색: Ctrl + F");
        Debug.Log("테이블 전환: 1~4");
        Debug.Log("필터 리셋: Ctrl + R");
    }

    [ContextMenu("Debug/Print Input Status")]
    public void CMD_PrintInputStatus()
    {
        bool actionsEnabled = inputActions?.DebugInventory.enabled ?? false;
        Debug.Log($"InputActions 생성됨: {inputActions != null}");
        Debug.Log($"DebugInventory Map 활성화: {actionsEnabled}");
        Debug.Log($"UI 열림 상태: {isUIOpen}");
        Debug.Log($"DebugUI 참조: {(debugUI != null ? "OK" : "NULL")}");
    }
    #endregion
}