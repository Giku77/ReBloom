/// <summary>
/// 드롭 목적지 인터페이스
/// </summary>
public interface IDropTarget
{
    /// <summary>
    /// 이 드래그 컨텍스트를 받을 수 있는지 판단
    /// </summary>
    bool CanAcceptDrop(DragContext context);

    /// <summary>
    /// 실제 드롭 처리
    /// </summary>
    void HandleDrop(DragContext context);
}