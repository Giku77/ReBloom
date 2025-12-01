using Cysharp.Threading.Tasks;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private TutorialSequence introSequence;
    [SerializeField] private CameraController cutsceneCamera; // 아래에서 설명

    private void Awake()
    {
        I = this;
    }

    public async UniTask RunIntroAsync()
    {
        await RunSequence(introSequence);
    }

    public async UniTask RunSequence(TutorialSequence seq)
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 플레이어 조작 잠금
        //PlayerInputBlocker.I.SetBlocked(true);

        foreach (var step in seq.steps)
        {
            switch (step.type)
            {
                case TutorialStepType.ShowDialogue:
                {
                    string text = Localize(step.stringKey); // StringTable에서 꺼내기
                    await dialogueUI.ShowLineAsync(text);
                    break;
                }

                case TutorialStepType.WaitSeconds:
                    await UniTask.Delay((int)(step.duration * 1000), cancellationToken: token);
                    break;

                case TutorialStepType.MoveCamera:
                    await cutsceneCamera.MoveToTargetAsync(step.cameraTarget, step.cameraOffset, step.duration);
                    break;

                case TutorialStepType.PlayAnimation:
                    if (step.targetAnimator != null && !string.IsNullOrEmpty(step.animStateName))
                        step.targetAnimator.Play(step.animStateName);
                    break;

                case TutorialStepType.WaitGameEvent:
                    await WaitForGameEvent(step.gameEventId, token);
                    break;

                // WaitKey는 DialogueUI에서 처리해도 되고, 따로 만들고 싶으면 추가
            }
        }

        //PlayerInputBlocker.I.SetBlocked(false);
        dialogueUI.Hide();   // 마지막에 닫기
    }

    // 임시 예시 – 실제론 이벤트 시스템 연결
    private async UniTask WaitForGameEvent(string eventId, System.Threading.CancellationToken token)
    {
        // await UniTask.WaitUntil(
        //     () => GameEventBus.HasFired(eventId),
        //     cancellationToken: token);
    }

    private string Localize(string key)
    {
        // 네가 쓰는 Localization 시스템에 맞게 구현
        return key; // 임시
    }
}
