using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private List<UIBase> uiList = new List<UIBase>();

    private readonly Dictionary<UIType, IGameUI> uiDict = new();
    private readonly Stack<UIType> escStack = new();  

    public bool IsInUIMode => escStack.Count > 0;

    private bool isBlockedInput = false;

    public bool IsBlockedInput => isBlockedInput;

    public bool IsGamePaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var ui in uiList)
        {
            if (ui == null) continue;
            if (!uiDict.ContainsKey(ui.Type))
            {
                uiDict.Add(ui.Type, ui);
            }
            else
            {
                Debug.LogWarning($"[UIManager] UIType {ui.Type}이 중복 등록됨");
            }
        }
    }

    private void Start()
    {
        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (ui.Layer == UILayer.HUD)
            {
                ui.Show();
            }
        }

        UpdateInputLock();
    }

    public void ToggleUI(UIType type)
    {
        if (!uiDict.TryGetValue(type, out var ui)) return;

        Debug.Log("[UIManager] 토글 UI 호출");

        if (ui.IsOpen)
        {
            Debug.Log($"[UIManager] Hiding UI: {type}");
            HideUI(type);
        }
        else
        {
            Debug.Log($"[UIManager] Showing UI: {type}");
            ShowUI(type);
        }
    }

    public void ShowUI(UIType type)
    {
        if (IsGamePaused && !IsPauseUI(type))
        {
            Debug.Log($"[UIManager] Pause 중 UI 차단: {type}");
            return;
        }

        if (!uiDict.TryGetValue(type, out var ui))
        {
            Debug.LogError($"[UIManager] UIType {type}이 uiDict에 없음!");
            return;
        }

        if (ui.Layer == UILayer.Modal)
        {
            CloseAllModalsExcept(type);
        }

        ui.Show();

        if (ui.BlocksGameplayInput)
        {
            PushToEscStack(type);
            UpdateInputLock();
        }
    }

    public void CloseAllUIs()
    {
        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (ui.IsOpen)
            {
                ui.Hide();
                RemoveFromEscStack(kvp.Key);
            }
        }
        UpdateInputLock();
    }

    private void CloseAllModalsExcept(UIType except)
    {
        foreach (var kvp in uiDict)
        {
            var other = kvp.Value;
            if (other.Layer == UILayer.Modal && other.IsOpen && kvp.Key != except)
            {
                other.Hide();
                RemoveFromEscStack(kvp.Key);
            }
        }
    }

    public void HideUI(UIType type)
    {
        if (!uiDict.TryGetValue(type, out var ui)) return;
        if (!ui.IsOpen) return;

        ui.Hide();
        RemoveFromEscStack(type);
        UpdateInputLock();
    }

    public void OnEscPressed()
    {
        if (escStack.Count > 0)
        {
            var top = escStack.Pop();
            if (uiDict.TryGetValue(top, out var ui))
            {
                ui.Hide();
            }
            UpdateInputLock();
        }
        else
        {
            Debug.Log("[UIManager] 게임 일시정지 ESC 호출");
            ShowUI(UIType.GamePause);
        }
    }

    private void PushToEscStack(UIType type)
    {
        if (!escStack.Contains(type))
            escStack.Push(type);
    }

    
    public void SetBlockingInput(bool isBlocked)
    {
        isBlockedInput = isBlocked;
        UpdateInputLock();
    }


    private void RemoveFromEscStack(UIType type)
    {
        if (!escStack.Contains(type)) return;

        var temp = new Stack<UIType>();
        while (escStack.Count > 0)
        {
            var t = escStack.Pop();
            if (t != type)
                temp.Push(t);
        }

        while (temp.Count > 0)
            escStack.Push(temp.Pop());
    }

    private void UpdateInputLock()
    {
        bool uiBlocksGameplay = false;

        // 커서 정책 집계
        UICursorMode cursorMode = UICursorMode.KeepGameplayLock;
        bool lockZoom = false;

        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (!ui.IsOpen) continue;

            if (ui.Layer == UILayer.HUD)
                continue;

            if (ui.BlocksGameplayInput)
                uiBlocksGameplay = true;

            // CursorMode: Unlock이 하나라도 있으면 그걸로
            if (ui is UIBase uibase)
            {
                if (uibase.CursorMode == UICursorMode.UnlockAndShow)
                    cursorMode = UICursorMode.UnlockAndShow;

                if (uibase.LocksCameraZoom)
                    lockZoom = true;
            }
        }

        // HUD 숨김/표시 (원래 로직 유지 가능)
        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (ui.Layer != UILayer.HUD) continue;

            if (uiBlocksGameplay && ui.IsOpen) ui.Hide();
            else if (!uiBlocksGameplay && !ui.IsOpen) ui.Show();
        }

        // 커서/락 상태는 CursorMode로 결정
        switch (cursorMode)
        {
            case UICursorMode.KeepGameplayLock:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case UICursorMode.UnlockAndShow:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case UICursorMode.UnlockAndHide:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
                break;
        }

        // 카메라 줌 잠금도 분리
        var cam = Camera.main ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
        if (cam != null)
            cam.isZoomLocked = lockZoom;
    }

    public T GetUI<T>(UIType type) where T : class, IGameUI
    {
        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (ui.Type == type && ui is T typedUI)
            {
                return typedUI;
            }
        }
        return null;
    }

    public bool IsUIOpen(UIType type)
    {
        if (uiDict.TryGetValue(type, out var ui))
        {
            return ui.IsOpen;
        }
        return false;
    }

    private bool IsPauseUI(UIType type)
    {
        return type == UIType.GamePause || type == UIType.Setting;
    }

    public void SetPaused(bool paused)
    {
        IsGamePaused = paused;
    }
}