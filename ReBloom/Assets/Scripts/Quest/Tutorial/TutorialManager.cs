using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private int introTutorialId = 1101001;

    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;

    private TutorialDB tutorialDb;

    private CancellationTokenSource tutorialCts;
    private bool isRunning;
    private bool hasLocalPlayerBinding;

    private int resumeTutorialId = 0;
    private bool introCompleted = false;

    public int ResumeTutorialId => resumeTutorialId;
    public bool IntroCompleted => introCompleted;

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        I = this;

        tutorialDb = new TutorialDB();
        tutorialDb.LoadFromBG();
        TryBindExistingLocalPlayer();
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += UnbindLocalPlayer;
        TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= UnbindLocalPlayer;
    }

    private void ShowTutorialText(string text)
    {
        if (tutorialText == null || tutorialPanel == null) return;

        tutorialPanel.SetActive(true);
        tutorialText.text = text;
    }

    private void ClearTutorialText()
    {
        if (tutorialText == null || tutorialPanel == null) return;

        tutorialText.text = string.Empty;
        tutorialPanel.SetActive(false);
    }

    private async void Start()
    {
        await EnsurePlayerBindingAsync();

        if (IsNetworkedSession)
        {
            introCompleted = true;
            resumeTutorialId = 0;
            playerController?.SetBlocked(false);
            enabled = false;
            return;
        }

        if (introCompleted)
        {
            playerController?.SetBlocked(false);
            return;
        }

        int startId = (resumeTutorialId > 0) ? resumeTutorialId : introTutorialId;
        var destroyToken = this.GetCancellationTokenOnDestroy();
        tutorialCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

        try
        {
            isRunning = true;
            await RunTutorialChainAsync(startId, tutorialCts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[TutorialManager] Tutorial cancelled (scene unload, play stop, or skip).");
        }
        finally
        {
            isRunning = false;

            VoiceManager.I?.Stop();

            if (this != null && gameObject.scene.isLoaded)
            {
                dialogueUI.Hide();
                ClearTutorialText();
            }

            playerController?.SetBlocked(false);

            tutorialCts?.Dispose();
            tutorialCts = null;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current == null) return;
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SkipTutorial();
        }
#endif
    }

    public void SkipTutorial()
    {
        if (tutorialCts != null && !tutorialCts.IsCancellationRequested)
        {
            Debug.Log("[TutorialManager] SkipTutorial called.");
            tutorialCts.Cancel();
        }
        introCompleted = true;
    }

    public void SetTutorialState(int resumeId, bool completed)
    {
        resumeTutorialId = resumeId;
        introCompleted = completed;
    }

    public async UniTask RunTutorialChainAsync(int startTutorialId, CancellationToken token)
    {
        int currentId = startTutorialId;
        resumeTutorialId = currentId;
        AutoSaveService.I?.RequestSave("Tutorial start/resume");

        while (currentId != 0 &&
               !token.IsCancellationRequested &&
               tutorialDb.TryGetTutorial(currentId, out var node))
        {
            if (playerController == null)
            {
                await EnsurePlayerBindingAsync();
                if (playerController == null)
                {
                    Debug.LogWarning("[TutorialManager] Local PlayerController binding is missing.");
                    break;
                }
            }

            playerController.SetBlocked(!node.IsControllable);

            string text = Localize(node.TutorialTextID);

            bool showCharacterImg = node.TextType == TutorialTextType.DialogueAndImg;
            bool waitForNextInput = node.Condition == TutorialConditionType.NextImmediately;
            bool showPoppiImage = node.TextType == TutorialTextType.DialogueAndPoppiImg;
            bool showNextHint = waitForNextInput;

            VoiceManager.I?.Stop();

            int varcoId = 0;
            if (tutorialDb.TryGetString(node.TutorialTextID, out var tutorialString))
            {
                varcoId = tutorialString.VarcoID;

                if (tutorialString.VarcoID > 0)
                {
                    VoiceManager.I?.PlayVoice(tutorialString.VarcoID);
                }
            }

            if (showNextHint)
            {
                ClearTutorialText();

                await dialogueUI.ShowLineAsync(
                    text,
                    varcoId,
                    showCharacterImg,
                    showPoppiImage,
                    waitForNextInput,
                    showNextHint,
                    cancellationToken: token);
            }
            else
            {
                dialogueUI.Hide();
                ShowTutorialText(text);
            }

            if (token.IsCancellationRequested)
                break;

            switch (node.Condition)
            {
                case TutorialConditionType.NextImmediately:
                    break;

                case TutorialConditionType.WaitExternal:
                    await WaitForActionAsync(node.ConditionObjectID, token);
                    break;

                case TutorialConditionType.WaitObjectEvent:
                    await WaitForTargetAsync(node.ConditionObjectID, token);
                    break;
            }

            currentId = node.NextTutorialID;
            resumeTutorialId = currentId;
            AutoSaveService.I?.RequestSave("Tutorial progress");
        }

        if (!token.IsCancellationRequested && currentId == 0)
        {
            introCompleted = true;
            resumeTutorialId = 0;
            AutoSaveService.I?.RequestSave("Tutorial completed");
        }
    }

    private async UniTask WaitForActionAsync(int actionId, CancellationToken token)
    {
        var actionIdType = (TutorialActionId)actionId;
        var tcs = new UniTaskCompletionSource();

        void Handler(int fired)
        {
            if (actionIdType == TutorialActionId.None || fired == (int)actionIdType)
            {
                TutorialEventBus.OnActionConditionSatisfied -= Handler;
                tcs.TrySetResult();
            }
        }

        TutorialEventBus.OnActionConditionSatisfied += Handler;

        using (token.Register(() =>
        {
            TutorialEventBus.OnActionConditionSatisfied -= Handler;
            tcs.TrySetCanceled();
        }))
        {
            await tcs.Task;
        }
    }

    private async UniTask WaitForTargetAsync(int targetId, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource();

        void Handler(int firedId)
        {
            if (firedId == targetId)
            {
                TutorialEventBus.OnTargetConditionSatisfied -= Handler;
                tcs.TrySetResult();
            }
        }

        TutorialEventBus.OnTargetConditionSatisfied += Handler;

        using (token.Register(() =>
        {
            TutorialEventBus.OnTargetConditionSatisfied -= Handler;
            tcs.TrySetCanceled();
        }))
        {
            await tcs.Task;
        }
    }

    private string Localize(int textId)
    {
        tutorialDb.TryGetString(textId, out var localizedText);
        if (PlatformManager.Instance != null && PlatformManager.Instance.IsMobile)
        {
            return localizedText != null ? localizedText.MobileTextKR : "-";
        }
        return localizedText != null ? localizedText.TextKR : "-";
    }

    private async UniTask EnsurePlayerBindingAsync()
    {
        TryBindExistingLocalPlayer();

        if (!IsNetworkedSession)
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
            return;
        }

        if (playerController != null)
        {
            hasLocalPlayerBinding = true;
            return;
        }

        await UniTask.WaitUntil(() => hasLocalPlayerBinding || playerController != null, cancellationToken: this.GetCancellationTokenOnDestroy());
        TryBindExistingLocalPlayer();
    }

    private void BindLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null)
            return;

        playerController = playerObj.GetComponent<PlayerController>();
        hasLocalPlayerBinding = playerController != null;
    }

    private void UnbindLocalPlayer()
    {
        hasLocalPlayerBinding = false;
        playerController = null;
    }

    private void TryBindExistingLocalPlayer()
    {
        if (!IsNetworkedSession)
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
            hasLocalPlayerBinding = playerController != null;
            return;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null)
            return;

        var localPlayerObject = nm.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject != null)
            BindLocalPlayer(localPlayerObject.gameObject);
    }
}