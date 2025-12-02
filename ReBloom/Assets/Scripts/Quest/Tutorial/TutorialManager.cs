using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private PlayerController playerController;
    //[SerializeField] private CameraController cutsceneCamera; // 카메라 튜토리얼 필요하면 나중에 추가
    [SerializeField] private int introTutorialId = 1101001;

    private TutorialDB tutorialDb;

    private void Awake()
    {
        I = this;

        tutorialDb = new TutorialDB();
        tutorialDb.LoadFromBG();
    }

    private async void Start()
    {
        try
        {
            await RunTutorialChainAsync(introTutorialId);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[TutorialManager] Tutorial cancelled (scene unload or play stop).");
        }
    }

    public async UniTask RunTutorialChainAsync(int startTutorialId)
    {
        var token = this.GetCancellationTokenOnDestroy();
        int currentId = startTutorialId;

        while (currentId != 0 && tutorialDb.TryGetTutorial(currentId, out var node))
        {
            playerController.SetBlocked(!node.IsControllable);

            string text = Localize(node.TutorialTextID);
            
            bool showCharacterImg = node.TextType == TutorialTextType.DialogueAndImg;

            bool waitForNextInput =
                node.Condition == TutorialConditionType.NextImmediately;

            bool showNextHint = waitForNextInput;

            await dialogueUI.ShowLineAsync(
                text,
                showCharacterImg,
                waitForNextInput,
                showNextHint);

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

        dialogueUI.Hide();
        playerController.SetBlocked(false);
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
