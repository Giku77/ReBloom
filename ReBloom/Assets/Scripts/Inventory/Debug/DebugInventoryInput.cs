using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// 디버그 인벤토리 입력 처리
/// InputManager의 공유 InputActions 사용
/// </summary>
public class DebugInventoryInput : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private bool enableInReleaseBuild = false;

    [Header("Ref")]
    [SerializeField] private DebugInventoryUI debugUI;

    private InputSystem_Actions inputActions; // 참조만 저장
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

        //// DebugInventoryUI 자동 찾기
        //debugUI = GetComponent<DebugInventoryUI>();
        //if (debugUI == null)
        //{
        //    debugUI = FindFirstObjectByType<DebugInventoryUI>();
        //}

        //if (debugUI == null)
        //{
        //    Debug.LogError("[DebugInventoryInput] DebugInventoryUI를 찾을 수 없습니다!");
        //    enabled = false;
        //    return;
        //}

        // 이벤트 연결
        SubscribeInputActions();

        Debug.Log("[DebugInventoryInput] 초기화 완료 (공유 InputActions 사용)");
    }

    private void OnDestroy()
    {
        UnsubscribeInputActions();
    }

    #region Input Actions 이벤트 구독
    /// <summary>
    /// Input Actions 이벤트 연결
    /// </summary>
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
    }

    /// <summary>
    /// Input Actions 이벤트 구독 해제
    /// </summary>
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
        debugUI.ToggleUI();
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
        // TODO: TMP_InputField.ActivateInputField() 호출
        Debug.Log("[디버그 인벤토리] 검색창 포커스");
    }

    private void SwitchToTable(ItemTableType tableType)
    {
        // TODO: DebugInventoryUI에 SwitchTable 메서드 추가 필요

        switch (tableType)
        {
            case ItemTableType.Consumable:
                debugUI.btnConsumable.interactable = true;
                break;
            case ItemTableType.Tool:
                debugUI.btnTool.interactable = true;
                break;
            case ItemTableType.Protective:
                debugUI.btnProtective.interactable = true;
                break;
            case ItemTableType.Misc:
                debugUI.btnMisc.interactable = true;
                break;
            default:
                break;
        }

        Debug.Log($"[디버그 인벤토리] {tableType} 테이블로 전환");
    }

    private void ResetFilters()
    {
        if (debugUI != null)
        {
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
        Debug.Log($"Input Actions 활성화: {actionsEnabled}");
        Debug.Log($"UI 열림 상태: {isUIOpen}");
        Debug.Log($"DebugUI 참조: {(debugUI != null ? "OK" : "NULL")}");
    }
    #endregion
}