using System;

public static class TutorialEventBus
{
    /// <summary>
    /// ConditionType=2 같은 “행동 완료” 이벤트.
    /// 예: "MovedOnce", "OpenedInventory" 같은 걸 enum/int로 표현 가능
    /// </summary>
    public static event Action<int> OnActionConditionSatisfied;

    /// <summary>
    /// ConditionType=3 같은 “특정 대상 ID 조건” 이벤트.
    /// 예: 건물 ID, 퀘스트 ID 등
    /// </summary>
    public static event Action<int> OnTargetConditionSatisfied;

    public static void RaiseAction(int actionId)
        => OnActionConditionSatisfied?.Invoke(actionId);

    public static void RaiseTarget(int targetId)
        => OnTargetConditionSatisfied?.Invoke(targetId);
}
