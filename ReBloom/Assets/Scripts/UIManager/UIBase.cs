using UnityEngine;

public enum UICursorMode
{
    KeepGameplayLock,  // 커서/락 그대로 유지
    UnlockAndShow,     // 커서 풀고 보이기
    UnlockAndHide      // (거의 안 쓰지만) 풀되 숨김
}
public abstract class UIBase : MonoBehaviour, IGameUI
{
    [Header("UI Setting")]
    [SerializeField] private UIType type;
    [SerializeField] private UILayer layer = UILayer.Modal;
    [SerializeField] private bool blocksGameplayInput = true;
    [SerializeField] private GameObject root; // 실제로 On/Off 할 루트 (null이면 자기 자신)
    [SerializeField] private UICursorMode cursorMode = UICursorMode.UnlockAndShow;
    [SerializeField] private bool locksCameraZoom = true;

    public UIType Type => type;
    public UILayer Layer => layer;
    public bool BlocksGameplayInput => blocksGameplayInput;
    public bool IsOpen { get; private set; }

    public UICursorMode CursorMode => cursorMode;
    public bool LocksCameraZoom => locksCameraZoom;

    protected virtual void Awake()
    {
        if (root == null)
            root = gameObject;

        root.SetActive(false); 
        IsOpen = false;
    }

    public virtual void Show()
    {
        if (IsOpen) return;
        IsOpen = true;
        root.SetActive(true);
        OnShow();

        Debug.Log("[UIBase] UIBase SHOW 호출");
    }

    public virtual void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;
        root.SetActive(false);
        OnHide();
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
