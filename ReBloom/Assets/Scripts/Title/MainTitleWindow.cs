using System.Collections.Generic;
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
    }

    private void Start()
    {
        SoundManager.I.PlayTitleBGM();
        RefreshContinueButtonStateAsync().Forget();
    }

    public async UniTask<IReadOnlyList<WorldSlotMetaDTO>> GetAvailableWorldSlotsAsync()
    {
        if (SaveManager.I == null)
            return Array.Empty<WorldSlotMetaDTO>();

        return await SaveManager.I.ListWorldSlotsAsync();
    }

    public async UniTask<string> GetSuggestedNewSlotIdAsync(int maxSlots = 8)
    {
        if (SaveManager.I == null)
            return GameStartContext.SlotId;

        return await SaveManager.I.SuggestNextSlotIdAsync(maxSlots);
    }

    public void SelectWorldSlot(string slotId, string displayName = null)
    {
        if (SaveManager.I != null)
        {
            SaveManager.I.SetActiveSlot(slotId, displayName);
            return;
        }

        GameStartContext.SlotId = string.IsNullOrWhiteSpace(slotId) ? GameStartContext.SlotId : slotId.Trim();
        GameStartContext.SlotDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? GameStartContext.SlotId
            : displayName.Trim();
    }

    private async UniTaskVoid RefreshContinueButtonStateAsync()
    {
        var slots = await GetAvailableWorldSlotsAsync();
        hasSave = slots.Count > 0;

        if (hasSave && SaveManager.I != null)
        {
            var selected = slots[0];
            SaveManager.I.SetActiveSlot(selected.slotId, selected.displayName);
        }

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
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }
    }

    public void OnGameStartButtonClicked()
    {
        if (!hasSave)
        {
            StartSelectedNewGameAsync().Forget();
            return;
        }

        OpenPopup();
    }

    public async UniTask<bool> StartNewGameWithSlotAsync(string slotId, string displayName = null)
    {
        SelectWorldSlot(slotId, displayName);

        string resolvedSlotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        GameStartContext.StartMode = GameStartContext.Mode.NewGame;
        GameStartContext.SlotId = resolvedSlotId;

        if (SaveManager.I != null)
            await SaveManager.I.ResetSlotAsync(resolvedSlotId, saveDefaultImmediately: false);

        SceneManager.LoadScene("LoadingScene");
        return true;
    }

    public async UniTask<bool> ContinueWithSlotAsync(string slotId, string displayName = null)
    {
        SelectWorldSlot(slotId, displayName);

        string resolvedSlotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        if (SaveManager.I != null)
        {
            bool slotHasSave = await SaveManager.I.HasSaveAsync(resolvedSlotId);
            if (!slotHasSave)
            {
                await OnNotImplementedButtonClickeddAsync();
                return false;
            }
        }

        GameStartContext.StartMode = GameStartContext.Mode.Continue;
        GameStartContext.SlotId = resolvedSlotId;
        SceneManager.LoadScene("LoadingScene");
        return true;
    }

    public void OnContinueButtonClicekd()
    {
        ContinueWithSelectedSlotAsync().Forget();
    }

    private async UniTaskVoid StartSelectedNewGameAsync()
    {
        string slotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        await StartNewGameWithSlotAsync(slotId, GameStartContext.SlotDisplayName);
    }

    private async UniTaskVoid ContinueWithSelectedSlotAsync()
    {
        string slotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        await ContinueWithSlotAsync(slotId, GameStartContext.SlotDisplayName);
    }

    public void OnSettingButtonClicked()
    {
        manager.ChangeWindow(settingWindow);
    }

    private async UniTaskVoid LoadContinueAsync()
    {
        string slotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        await ContinueWithSlotAsync(slotId, GameStartContext.SlotDisplayName);
    }

    private async UniTask OnNotImplementedButtonClickeddAsync()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        if (ToastService.I != null) ToastService.I.Show("저장 데이터가 없습니다.");
        await UniTask.CompletedTask;
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
            EventSystem.current.SetSelectedGameObject(executeButton.gameObject);
    }

    private void ClosePopup()
    {
        popup.SetActive(false);
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsMobile)
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
    }

    private async void OnExecuteButtonClicked()
    {
        string slotId = SaveManager.I != null ? SaveManager.I.ActiveSlotId : GameStartContext.SlotId;
        GameStartContext.StartMode = GameStartContext.Mode.NewGame;
        GameStartContext.SlotId = slotId;
        await SaveManager.I.ResetSlotAsync(slotId, saveDefaultImmediately: false);
        SceneManager.LoadScene("LoadingScene");
    }

    private void OnCancelButtonClicked()
    {
        ClosePopup();
    }
}

