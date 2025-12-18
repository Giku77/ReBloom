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
    [SerializeField] private TextMeshProUGUI toastMessage;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitGameButton;

    [SerializeField] private WindowManager manager;
    [SerializeField] private Window settingWindow;

    public bool initialized = false;

    private CancellationTokenSource cts;

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnGameStartButtonClicked);
        continueGameButton.onClick.AddListener(OnContinueButtonClicekd);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitButtonClicked);


    }

    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);

        cts = new CancellationTokenSource();
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
        toastMessage.gameObject.SetActive(false);

        SoundManager.I.PlayTitleBGM();
    }

    private void Update()
    {
        if (UIButtonHoverDeselect.IsMouseHoveringButton)
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
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnContinueButtonClicekd()
    { 
        OnNotImplementedButtonClickeddAsync().Forget();
    }

    public void OnSettingButtonClicked()
    {
        manager.ChangeWindow(settingWindow);
    }

    private async UniTask OnNotImplementedButtonClickeddAsync()
    {
        toastMessage.gameObject.SetActive(true);

        toastMessage.text = "추후 구현 예정입니다.";

        try
        {
            await UniTask.Delay(2000, cancellationToken: cts.Token);
            toastMessage.gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[MainTitleWindow] 메세지 안전하게 취소");
        }
    }

    public void OnQuitButtonClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
