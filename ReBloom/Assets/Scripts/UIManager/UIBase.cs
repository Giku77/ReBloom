using UnityEngine;

public abstract class UIBase : MonoBehaviour, IGameUI
{
    [Header("UI Setting")]
    [SerializeField] private UIType type;
    [SerializeField] private UILayer layer = UILayer.Modal;
    [SerializeField] private bool blocksGameplayInput = true;
    [SerializeField] private GameObject root; // 실제로 On/Off 할 루트 (null이면 자기 자신)

    public UIType Type => type;
    public UILayer Layer => layer;
    public bool BlocksGameplayInput => blocksGameplayInput;
    public bool IsOpen { get; private set; }

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
