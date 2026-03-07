using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PopupType
{
    None,
    Title,
    QuitGame,
    Escape,
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
    [SerializeField] private Button escapeButton;

    [Header("팝업 UI")]
    [SerializeField] private GameObject popup;
    [SerializeField] private TextMeshProUGUI popupText;

    [Header("References")]
    [SerializeField] private PlayerStats player;

    private PopupType currentPopupType = PopupType.None;
    private readonly string titlePopUpText = "타이틀로 돌아가시겠습니까?";
    private readonly string quitGamePopUpText = "게임을 정말 종료하시겠습니까?";
    private readonly string escapePopUpText = "모든 아이템을 드랍합니다. 탈출하시겠습니까?";

    protected override void Awake()
    {
        base.Awake();

        gameResumeButton.onClick.AddListener(OnGameResumeButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        titleButton.onClick.AddListener(OnTitleButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);
        escapeButton.onClick.AddListener(OnEscapeButtonClicked);

        popup.SetActive(false);
    }

    public void Toggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;

        UIManager.Instance?.ToggleUI(Type);
        Debug.Log("[GamePauseUI] 게임일시정지 UI 토글 호출");
    }

    protected override void OnShow()
    {
        Time.timeScale = 0f;
        SoundManager.I?.PlayOpenInventory();
        UIManager.Instance?.SetPaused(true);
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += UnbindLocalPlayer;
        TryBindFromExistingOwner();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= UnbindLocalPlayer;
        player = null;
    }

    private void BindLocalPlayer(GameObject playerGo)
    {
        if (playerGo == null)
            return;

        player = playerGo.GetComponent<PlayerStats>();
    }

    private void UnbindLocalPlayer()
    {
        player = null;
    }

    private void TryBindFromExistingOwner()
    {
        var nos = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var no in nos)
        {
            if (!no.IsOwner)
                continue;
            if (no.GetComponent<PlayerController>() == null)
                continue;

            BindLocalPlayer(no.gameObject);
            return;
        }
    }

    protected override void OnHide()
    {
        Time.timeScale = 1f;
        SoundManager.I?.PlayCloseInventory();
        UIManager.Instance?.SetPaused(false);

        if (popup.activeSelf)
            ClosePopup();
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
        currentPopupType = PopupType.Title;
        OpenPopup();
    }

    private void OnQuitGameButtonClicked()
    {
        currentPopupType = PopupType.QuitGame;
        OpenPopup();
    }

    private void OnEscapeButtonClicked()
    {
        currentPopupType = PopupType.Escape;
        OpenPopup();
    }

    private void OpenPopup()
    {
        popup.SetActive(true);

        executeButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        SoundManager.I?.PlayUIClick();

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
            case PopupType.Escape:
                popupText.text = escapePopUpText;
                executeButton.onClick.AddListener(Escape);
                cancelButton.onClick.AddListener(ClosePopup);
                break;
        }
    }

    private void ClosePopup()
    {
        executeButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        popupText.text = string.Empty;

        SoundManager.I?.PlayUIClick();
        popup.SetActive(false);
        currentPopupType = PopupType.None;
    }

    private async void LoadTitleScene()
    {
        SoundManager.I?.PlayUIClick();
        await ReturnToTitleAsync();
    }

    private async UniTask ReturnToTitleAsync()
    {
        Time.timeScale = 1f;
        UIManager.Instance?.SetPaused(false);
        popup.SetActive(false);
        currentPopupType = PopupType.None;

        if (AutoSaveService.I != null)
        {
            bool flushed = await AutoSaveService.I.FlushAsync();
            Debug.Log($"[GamePauseUI] AutoSave flush before title: {flushed}");
        }

        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && (networkManager.IsListening || networkManager.ShutdownInProgress))
        {
            Debug.Log($"[GamePauseUI] Returning to title. Shutting down session. server={networkManager.IsServer} client={networkManager.IsClient}");

            if (networkManager.IsListening)
                networkManager.Shutdown();

            await WaitForNetworkShutdownAsync(networkManager);
        }

        JoinCodeStore.Current = string.Empty;
        GameStartContext.StartMode = GameStartContext.Mode.Debug;
        SceneManager.LoadScene("TitleScene");
    }

    private static async UniTask WaitForNetworkShutdownAsync(NetworkManager networkManager)
    {
        const float timeoutSeconds = 10f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            bool isStillShuttingDown = networkManager != null && networkManager.ShutdownInProgress;
            bool isStillListening = networkManager != null && networkManager.IsListening;

            if (!isStillShuttingDown && !isStillListening)
                break;

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }

        Debug.Log($"[GamePauseUI] Shutdown wait finished. listening={networkManager != null && networkManager.IsListening} shutdownInProgress={networkManager != null && networkManager.ShutdownInProgress}");
        await UniTask.DelayFrame(2);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Escape()
    {
        if (player == null)
            return;

        SoundManager.I?.PlayUIClick();
        player.TakeDamage(100);
    }
}
