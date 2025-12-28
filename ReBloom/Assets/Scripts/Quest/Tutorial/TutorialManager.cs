using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
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

    private void Awake()
    {
        I = this;

        tutorialDb = new TutorialDB();
        tutorialDb.LoadFromBG();
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
        var destroyToken = this.GetCancellationTokenOnDestroy();
        tutorialCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

        try
        {
            isRunning = true;
            await RunTutorialChainAsync(introTutorialId, tutorialCts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[TutorialManager] Tutorial cancelled (scene unload, play stop, or skip).");
        }
        finally
        {
            isRunning = false;

            VoiceManager.I?.Stop();

            //유니티 플레이 후 비활성화 되는 문제로 인하여 If문 추가
            if (this != null && gameObject.scene.isLoaded)
            {
                dialogueUI.Hide();
                ClearTutorialText();
            }
            playerController.SetBlocked(false);

            tutorialCts.Dispose();
            tutorialCts = null;
        }
    }

    private void Update()
    {
        if (!isRunning) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SkipTutorial();
        }
    }

    public void SkipTutorial()
    {
        if (tutorialCts != null && !tutorialCts.IsCancellationRequested)
        {
            Debug.Log("[TutorialManager] SkipTutorial called.");
            tutorialCts.Cancel();
        }
    }

    public async UniTask RunTutorialChainAsync(int startTutorialId, CancellationToken token)
    {
        int currentId = startTutorialId;

        while (currentId != 0 &&
               !token.IsCancellationRequested &&
               tutorialDb.TryGetTutorial(currentId, out var node))
        {
            playerController.SetBlocked(!node.IsControllable);

            string text = Localize(node.TutorialTextID);

            bool showCharacterImg = node.TextType == TutorialTextType.DialogueAndImg;
            bool waitForNextInput = node.Condition == TutorialConditionType.NextImmediately;
            bool showPoppiImage = node.TextType == TutorialTextType.DialogueAndPoppiImg;
            bool showNextHint = waitForNextInput;

            //await dialogueUI.ShowLineAsync(
            //    text,
            //    showCharacterImg,
            //    waitForNextInput,
            //    showNextHint);

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
        return localizedText != null ? localizedText.TextKR : "-";
    }
}
