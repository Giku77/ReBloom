using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainTitleWindow : Window
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitGameButton;

    [SerializeField] private WindowManager manager;
    [SerializeField] private Window settingWindow;

    [Header("팝업 UI")]
    [SerializeField] private GameObject popup;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button cancelButton;

    public bool initialized = false;
    private bool hasSave = false;

    private CancellationTokenSource cts;

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnGameStartButtonClicked);
        continueGameButton.onClick.AddListener(OnContinueButtonClicekd);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitButtonClicked);
        executeButton.onClick.AddListener(OnExecuteButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnEnable()
    {
        popup.SetActive(false);

        cts = new CancellationTokenSource();

        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsMobile)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }
    }

    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();

        //newGameButton.onClick?.RemoveAllListeners();
        //continueGameButton.onClick?.RemoveAllListeners();
        //settingButton.onClick?.RemoveAllListeners();
        //quitGameButton.onClick?.RemoveAllListeners();
    }

    private void Start()
    {

        SoundManager.I.PlayTitleBGM();

        RefreshContinueButtonStateAsync().Forget();
    }

    private async UniTaskVoid RefreshContinueButtonStateAsync()
    {
        hasSave = await SaveManager.I.HasSaveAsync("slot1");

        continueGameButton.interactable = hasSave; 
                                                   
    }

    private void Update()
    {
        if (popup.activeSelf)
            return;

        if (UIButtonHoverDeselect.IsMouseHoveringButton)
            return;

        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
            return;

        if (Keyboard.current == null)
            return;

        bool navigationKeyPressed =
            Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.downArrowKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame;

        if (navigationKeyPressed)
        {
            initialized = true;

            if (EventSystem.current.currentSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
            }
        }
    }

    public void OnGameStartButtonClicked()
    {
        if (!hasSave)
        {
            GameStartContext.StartMode = GameStartContext.Mode.NewGame;
            SaveManager.I?.ResetSlotAsync("slot1", saveDefaultImmediately: false).Forget();
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            OpenPopup();
        }

    }

    private async UniTaskVoid StartNewGameAsync()
    {
        await SaveManager.I.ResetSlotAsync("slot1", saveDefaultImmediately: false);
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnContinueButtonClicekd()
    {
        GameStartContext.StartMode = GameStartContext.Mode.Continue;
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnSettingButtonClicked()
    {
        manager.ChangeWindow(settingWindow);
    }

    private async UniTaskVoid LoadContinueAsync()
    {
        bool hasSave = await SaveManager.I.HasSaveAsync("slot1");
        if (!hasSave)
        {
            await OnNotImplementedButtonClickeddAsync();
            return;
        }

        SceneManager.LoadScene("LoadingScene");
    }

    private async UniTask OnNotImplementedButtonClickeddAsync()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        if (ToastService.I != null) ToastService.I.Show("저장 데이터가 없습니다.");
    }

    public void OnQuitButtonClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OpenPopup()
    {
        if (popup.activeSelf) return;

        popup.SetActive(true);
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsMobile)
        {
            EventSystem.current.SetSelectedGameObject(executeButton.gameObject);
        }
    }

    private void ClosePopup()
    {
        popup.SetActive(false);
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsMobile)
        {
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }
    }

    private async void OnExecuteButtonClicked()
    {
        GameStartContext.StartMode = GameStartContext.Mode.NewGame;
        await SaveManager.I.ResetSlotAsync("slot1", saveDefaultImmediately:false);
        SceneManager.LoadScene("LoadingScene");
    }

    private void OnCancelButtonClicked()
    {
        ClosePopup();
    }
}
