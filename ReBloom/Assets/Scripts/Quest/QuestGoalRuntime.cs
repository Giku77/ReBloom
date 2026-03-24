using System.Collections.Generic;
using UnityEngine;

public enum QuestEventType
{
    None = 0,
    InventoryChanged = 1,
    BuildingChanged = 2,
    StageChanged = 3,
    Interacted = 4,
    GreeningChanged = 5
}

public readonly struct QuestEvent
{
    public QuestEventType Type { get; }
    public int ObjectId { get; }
    public int Value { get; }

    private QuestEvent(QuestEventType type, int objectId, int value)
    {
        Type = type;
        ObjectId = objectId;
        Value = value;
    }

    public static QuestEvent InventoryChanged() => new(QuestEventType.InventoryChanged, 0, 0);
    public static QuestEvent BuildingChanged(int objectId) => new(QuestEventType.BuildingChanged, objectId, 0);
    public static QuestEvent StageChanged(int stageId) => new(QuestEventType.StageChanged, stageId, 0);
    public static QuestEvent Interacted(int objectId) => new(QuestEventType.Interacted, objectId, 0);
    public static QuestEvent GreeningChanged(float greening) => new(QuestEventType.GreeningChanged, 0, Mathf.FloorToInt(greening));
}

public readonly struct QuestGoalContext
{
    public GameInventory Inventory { get; }
    public StageDetector StageDetector { get; }

    public QuestGoalContext(GameInventory inventory, StageDetector stageDetector)
    {
        Inventory = inventory;
        StageDetector = stageDetector;
    }

    public int GetItemCount(int itemId) => Inventory != null ? Inventory.GetItemCount(itemId) : 0;
    public int GetBuildCount(int arcId) => BuildManager.I != null ? BuildManager.I.GetCount(arcId) : 0;
    public int CurrentStageId => StageDetector != null && StageDetector.CurrentStage != null ? StageDetector.CurrentStage.StageID : 0;
    public int CurrentGreening => ResearchManager.I != null ? Mathf.FloorToInt(ResearchManager.I.CurrentGreening) : 0;
}

public interface IQuestGoal
{
    int GoalIndex { get; }
    QuestGoalType Type { get; }
    QuestGoal Data { get; }

    bool Sync(QuestGoalContext context);
    bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context);
    bool IsSatisfied();
    QuestGoalProgressDTO CaptureProgress();
    void RestoreProgress(QuestGoalProgressDTO progress);
}

public abstract class QuestGoalBase : IQuestGoal
{
    protected QuestGoalBase(QuestGoal data, int goalIndex)
    {
        Data = data;
        GoalIndex = goalIndex;
    }

    public int GoalIndex { get; }
    public QuestGoalType Type => Data.type;
    public QuestGoal Data { get; }

    public virtual bool Sync(QuestGoalContext context) => false;
    public virtual bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context) => false;
    public virtual bool IsSatisfied() => Data.currentCount >= Data.amount;

    public virtual QuestGoalProgressDTO CaptureProgress()
    {
        return new QuestGoalProgressDTO
        {
            goalIndex = GoalIndex,
            type = Type,
            objectId = Data.objectId,
            currentCount = Data.currentCount
        };
    }

    public virtual void RestoreProgress(QuestGoalProgressDTO progress)
    {
        if (progress == null)
            return;

        SetCurrentCount(progress.currentCount);
    }

    protected bool SetCurrentCount(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (Data.currentCount == clamped)
            return false;

        Data.currentCount = clamped;
        return true;
    }
}

public sealed class NoneQuestGoal : QuestGoalBase
{
    public NoneQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool IsSatisfied() => true;
}

public sealed class CollectQuestGoal : QuestGoalBase
{
    public CollectQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool Sync(QuestGoalContext context)
    {
        return SetCurrentCount(context.GetItemCount(Data.objectId));
    }

    public override bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context)
    {
        return questEvent.Type == QuestEventType.InventoryChanged && Sync(context);
    }
}

public sealed class CraftQuestGoal : QuestGoalBase
{
    public CraftQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool Sync(QuestGoalContext context)
    {
        return SetCurrentCount(context.GetBuildCount(Data.objectId));
    }

    public override bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context)
    {
        if (questEvent.Type != QuestEventType.BuildingChanged)
            return false;
        if (questEvent.ObjectId != Data.objectId)
            return false;

        return Sync(context);
    }
}

public sealed class EnterQuestGoal : QuestGoalBase
{
    public EnterQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool Sync(QuestGoalContext context)
    {
        int currentCount = context.CurrentStageId == Data.objectId ? Mathf.Max(1, Data.amount) : 0;
        return SetCurrentCount(currentCount);
    }

    public override bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context)
    {
        return questEvent.Type == QuestEventType.StageChanged && Sync(context);
    }
}

public sealed class InteractQuestGoal : QuestGoalBase
{
    public InteractQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context)
    {
        if (questEvent.Type != QuestEventType.Interacted)
            return false;
        if (questEvent.ObjectId != Data.objectId)
            return false;

        return SetCurrentCount(Mathf.Clamp(Data.currentCount + 1, 0, Mathf.Max(1, Data.amount)));
    }
}

public sealed class EndingQuestGoal : QuestGoalBase
{
    public EndingQuestGoal(QuestGoal data, int goalIndex) : base(data, goalIndex) { }

    public override bool Sync(QuestGoalContext context)
    {
        return SetCurrentCount(context.CurrentGreening);
    }

    public override bool HandleEvent(in QuestEvent questEvent, QuestGoalContext context)
    {
        return questEvent.Type == QuestEventType.GreeningChanged && Sync(context);
    }
}

public static class QuestGoalFactory
{
    public static List<IQuestGoal> CreateGoals(IReadOnlyList<QuestGoal> goalDataList)
    {
        var goals = new List<IQuestGoal>();
        if (goalDataList == null)
            return goals;

        for (int i = 0; i < goalDataList.Count; i++)
        {
            var goal = CreateGoal(goalDataList[i], i);
            if (goal != null)
                goals.Add(goal);
        }

        return goals;
    }

    public static IQuestGoal CreateGoal(QuestGoal goalData, int goalIndex)
    {
        if (goalData == null)
            return null;

        goalData.currentCount = 0;

        return goalData.type switch
        {
            QuestGoalType.None => new NoneQuestGoal(goalData, goalIndex),
            QuestGoalType.Collect => new CollectQuestGoal(goalData, goalIndex),
            QuestGoalType.Craft => new CraftQuestGoal(goalData, goalIndex),
            QuestGoalType.Enter => new EnterQuestGoal(goalData, goalIndex),
            QuestGoalType.Interact => new InteractQuestGoal(goalData, goalIndex),
            QuestGoalType.Ending => new EndingQuestGoal(goalData, goalIndex),
            _ => null
        };
    }
}
