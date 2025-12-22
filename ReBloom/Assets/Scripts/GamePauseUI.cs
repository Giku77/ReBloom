using Newtonsoft.Json.Bson;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PopupType
{ 
    None,
    Title,
    QuitGame
}


public class GamePauseUI : UIBase
{
    [Header("버튼 UI")]
    [SerializeField] private Button gameResumeButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button cancelButton;

    [Header("팝업 UI")]
    [SerializeField] private GameObject popup;
    [SerializeField] private TextMeshProUGUI popupText;

    private PopupType currentPopupType = PopupType.None;
    private string titlePopUpText = "타이틀로 돌아가시겠습니까?";
    private string quitGamePopUpText = "게임을 정말 종료하시겠습니까?";

    protected override void Awake()
    {
        base.Awake();

        gameResumeButton.onClick.AddListener(OnGameResumeButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        titleButton.onClick.AddListener(OnTitleButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);

        popup.SetActive(false);
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
        SoundManager.I?.PlayOpenInventory();
        UIManager.Instance?.SetPaused(true);
    }

    protected override void OnHide()
    {
        Time.timeScale = 1f;
        SoundManager.I?.PlayCloseInventory();
        UIManager.Instance?.SetPaused(false);
    }

    private void OnGameResumeButtonClicked()
    {
        UIManager.Instance?.HideUI(Type);
    }

    private void OnSettingButtonClicked()
    {
        Debug.Log("[GamePauseUI] 세팅버튼 클릭");
        UIManager.Instance?.ShowUI(UIType.Setting);
    }

    private void OnTitleButtonClicked()
    {
        Debug.Log("[GamePauseUI] 타이틀 버튼 클릭");
        //SceneManager.LoadScene("TitleScene");

        currentPopupType = PopupType.Title;
        OpenPopup();
    }

    private void OnQuitGameButtonClicked()
    {
        currentPopupType = PopupType.QuitGame;
        OpenPopup();
    }

    private void OpenPopup()
    {
        popup.SetActive(true);

        executeButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        switch (currentPopupType)
        {
            case PopupType.Title:
                popupText.text = titlePopUpText;
                executeButton.onClick.AddListener(LoadTitleScene);
                cancelButton.onClick.AddListener(ClosePopup);
                break;
            case PopupType.QuitGame:
                popupText.text = quitGamePopUpText;
                executeButton.onClick.AddListener(QuitGame);
                cancelButton.onClick.AddListener(ClosePopup);
                break;
        }
    }

    private void ClosePopup()
    { 
        executeButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        popupText.text = string.Empty;

        popup.SetActive(false);

        currentPopupType = PopupType.None;
    }

    private void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

    }

}
