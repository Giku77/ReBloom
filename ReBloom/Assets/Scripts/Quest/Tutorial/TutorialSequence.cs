using UnityEngine;

public enum TutorialStepType
{
    ShowDialogue,   // 대사 한 줄
    WaitSeconds,    // 시간 대기
    WaitKey,        // 특정 키 대기 (스킵 용)
    MoveCamera,     // 카메라 샷 전환
    PlayAnimation,  // 플레이어/ NPC 애니
    WaitGameEvent   // “건축 튜토 끝날 때까지” 같은 커스텀 이벤트
}

[System.Serializable]
public class TutorialStep
{
    public TutorialStepType type;

    public string stringKey;         // 대사/튜토 텍스트 키
    public float duration = 1f;      // WaitSeconds, 카메라 이동시간 등
    public string animStateName;     // 애니메이션 이름
    public Transform cameraTarget;   // 카메라가 볼 대상
    public Vector3 cameraOffset;     // 타겟 기준 오프셋
    public string gameEventId;       // "BuiltFirstCorridor" 같은 이벤트 ID
    public Animator targetAnimator;  // 애니 재생할 Animator (플레이어/NPC)
}

[CreateAssetMenu(menuName = "ReBloom/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    public string sequenceId;        // "Intro", "Build_Tutorial_1" 이런 식
    public TutorialStep[] steps;
}
