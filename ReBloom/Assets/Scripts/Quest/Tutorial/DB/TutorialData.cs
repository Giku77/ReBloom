public enum TutorialTextType 
{
    Dialogue = 1,   // 일반 대화창
    DialogueAndImg = 2,   // 일반 대화창과 캐릭터 이미지
    DialogueAndPoppiImg = 3 // 대화창과 포피 이미지
}

public enum TutorialActionId
{
    None = 0,
    MoveOnce = 1,       // WASD로 한 번이라도 이동
    PickupItem = 2,     // 아이템 하나 주움
    OpenInventory = 3,  // I키로 인벤토리 열기
    OpenBuildMode = 4,  // B키로 건축 모드 열기
    // 필요하면 계속 추가
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
    public int VarcoID;
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
