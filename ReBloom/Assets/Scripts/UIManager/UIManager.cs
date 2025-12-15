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
        if (!uiDict.TryGetValue(type, out var ui)) return;

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
            // 아무 모달도 안 열려 있을 때 ESC 정책 (예: 일시정지 열기)
            // ShowUI(UIType.Settings);
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
        bool uiBlocking = false;

        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;
            if (ui.IsOpen && ui.BlocksGameplayInput)
            {
                uiBlocking = true;
                break;
            }
        }

        foreach (var kvp in uiDict)
        {
            var ui = kvp.Value;

            if (ui.Layer == UILayer.HUD)
            {
                if (uiBlocking && ui.IsOpen)
                {
                    ui.Hide();  
                }
                else if (!uiBlocking && !ui.IsOpen)
                {
                    ui.Show(); 
                }
            }
        }

        // PlayerInput ActionMap 전환
        // var playerInput = FindFirstObjectByType<PlayerInput>();
        // if (playerInput != null)
        // {
        //     playerInput.SwitchCurrentActionMap(uiBlocking ? "UI" : "Gameplay");
        // }

        Cursor.lockState = uiBlocking ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiBlocking;

        Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = uiBlocking;
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
}
