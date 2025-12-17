using UnityEngine;

public class GamePauseUI : UIBase
{
    [SerializeField] private GameObject gamePauseUI;

    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;
        UIManager.Instance?.ToggleUI(Type);
        Debug.Log("[GamePuaseUI] 게임일시정지 UI 토글 호출");
    }

    protected override void OnShow()
    {
        Time.timeScale = 0f;
        UIManager.Instance.SetPaused(true);
    }

    protected override void OnHide()
    {
        Time.timeScale = 1f;
        UIManager.Instance.SetPaused(false);
    }

}
