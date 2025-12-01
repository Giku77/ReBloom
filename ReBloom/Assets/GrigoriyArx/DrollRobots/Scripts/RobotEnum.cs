/// <summary>
/// 로봇이 표현할 수 있는 감정 상태
/// </summary>
public enum RobotEmotion
{
    Neutral = 0,      // 중립
    Happy = 1,        // 행복
    Sad = 2,          // 슬픔
    Distrust = 3,     // 불신
    Wonder = 4,       // 궁금
    Death = 5,        // 사망
    Disgust = 6,      // 혐오/힘듦
    Evil = 7,         // 화남/악
    Cry = 8,          // 울음
    Love = 9          // 사랑
}

/// <summary>
/// 로봇 이동 상태
/// </summary>
public enum RobotMovementState
{
    Idle,             // 대기
    Walk,             // 걷기
    Run               // 달리기
}