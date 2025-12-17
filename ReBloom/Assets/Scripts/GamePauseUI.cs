using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePauseUI : UIBase
{
    [Header("버튼 UI")]
    [SerializeField] private Button gameResumeButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitGameButton;

    protected override void Awake()
    {
        base.Awake();

        gameResumeButton.onClick.AddListener(OnGameResumeButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        titleButton.onClick.AddListener(OnTitleButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);
    }

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

    private void OnGameResumeButtonClicked()
    {
        UIManager.Instance.HideUI(Type);
    }

    private void OnSettingButtonClicked()
    {
        Debug.Log("[GamePauseUI] 세팅버튼 클릭");
    }

    private void OnTitleButtonClicked()
    {
        Debug.Log("[GamePauseUI] 타이틀 버튼 클릭");
        SceneManager.LoadScene("TitleScene");
    }

    private void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

}
