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

    [Header("Auto Close Button (Mobile Only)")]
    [SerializeField] private bool autoAttachCloseButtonOnMobile = true;
    [SerializeField] private GameObject closeButtonPrefab;     
    [SerializeField] private Transform closeButtonParent;      

    private GameObject spawnedCloseButton;

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
        TryAttachCloseButton_MobileOnly();

        OnShow();
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

    private void TryAttachCloseButton_MobileOnly()
    {
        if (!autoAttachCloseButtonOnMobile) return;

        bool isMobile = PlatformManager.Instance != null
            ? PlatformManager.Instance.IsMobile
            : Application.isMobilePlatform;

        if (!isMobile) return;

        if (Layer != UILayer.Modal) return;

        if (closeButtonPrefab == null) return;

        if (spawnedCloseButton == null)
        {
            Transform parent = closeButtonParent != null ? closeButtonParent : root.transform;
            spawnedCloseButton = Instantiate(closeButtonPrefab, parent, false);
        }

        var binder = spawnedCloseButton.GetComponent<UICloseButton>();
        if (binder != null)
            binder.Bind(Type);
        else
            Debug.LogWarning($"[UIBase] closeButtonPrefab에 UICloseButton이 없습니다: {closeButtonPrefab.name}");
    }
}
