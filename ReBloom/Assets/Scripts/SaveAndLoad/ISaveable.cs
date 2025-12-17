public interface ISaveable
{
    /// 이 컴포넌트가 붙은 엔티티 GUID (SaveableEntity PersistentId)
    string EntityGuid { get; }

    /// 저장할 때 호출
    void Capture(SaveGameDTO save);

    /// 로드할 때 호출 (주로 “주입/연결” 단계에서 사용)
    void Restore(SaveGameDTO save);
}
