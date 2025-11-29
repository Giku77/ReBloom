public interface IGameUI
{
    UIType Type { get; }
    UILayer Layer { get; }
    bool IsOpen { get; }
    bool BlocksGameplayInput { get; }   // 열리면 입력/커서 잠글지 여부

    void Show();
    void Hide();
}

