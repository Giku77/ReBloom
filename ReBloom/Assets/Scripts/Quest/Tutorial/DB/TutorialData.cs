public enum TutorialTextType 
{
    Dialogue = 1,   // 중앙 대화박스 (예시)
    System = 2      // 상단/하단 안내문
}

public enum TutorialConditionType 
{
    NextImmediately = 1,   // 단순 다음 (or 다음 키 입력)
    WaitExternal = 2,      // 코드에서 CompleteTutorial 호출해줘야 함
    WaitObjectEvent = 3    // ConditionObjectID와 관련된 이벤트 대기
}

public class TutorialStringData
{
    public int TutorialStringID;
    public string TextKR;
    // 나중에 EN, JP 추가해도 됨
}

public class TutorialData
{
    public int TutorialID;
    public TutorialTextType TextType;
    public int TutorialTextID;
    public int NextTutorialID;
    public TutorialConditionType Condition;
    public int ConditionObjectID;
    public bool IsControllable;
}
