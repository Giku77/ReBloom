using UnityEngine;

/// <summary>
/// 로봇의 감정 표현을 관리 (눈, 입, 색상)
/// </summary>
public class RobotEmotionManager : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private Rob11ColorManager colorManager;
    [SerializeField] private EmotionChanger emotionChanger;

    private RobotEmotion currentEmotion = RobotEmotion.Neutral;

    /// <summary>
    /// 감정 설정 (색상, 눈, 입 모두 변경)
    /// </summary>
    public void SetEmotion(RobotEmotion emotion)
    {
        currentEmotion = emotion;
        int emotionIndex = (int)emotion;

        // 색상 변경
        colorManager?.ChangeBodyColor(emotionIndex);

        // 눈 표정 변경
        emotionChanger?.SetEmotionEyes(emotionIndex);

        // 입 표정 변경
        emotionChanger?.SetEmotionMouth(emotionIndex);
    }

    /// <summary>
    /// 현재 감정 가져오기
    /// </summary>
    public RobotEmotion GetCurrentEmotion()
    {
        return currentEmotion;
    }

    /// <summary>
    /// 중립 상태로 리셋
    /// </summary>
    public void ResetToNeutral()
    {
        SetEmotion(RobotEmotion.Neutral);
    }
}