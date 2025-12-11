using System.Collections.Generic;
using System;

public enum QuestGoalType
{
    None = 0,
    Collect = 1,
    Craft = 2,
    Enter = 3,
    Interact = 4
}

[Serializable]
public class QuestGoal
{
    public QuestGoalType type;
    public int objectId;
    public int amount;

    [NonSerialized] public int currentCount;

    public bool IsSatisfied(IInventoryProvider inv, StageDetector stageDetector)
    {
        switch (type)
        {
            case QuestGoalType.Collect:
                return inv.GetItemCount(objectId) >= amount;
            case QuestGoalType.Craft:
                return currentCount >= amount;
            case QuestGoalType.Enter:
                var currentStage = stageDetector.CurrentStage;
                if (currentStage != null)
                {
                    return currentStage.StageID == objectId;
                }
                return false;
            default:
                return true;
        }
    }
}

[Serializable]
public class QuestReward
{
    public int itemId;
    public int amount;
}

[Serializable]
public class QuestData
{
    public int questId;
    public string questName;
    public int questNameID;
    public int questTextID;
    public int formerQuestId;

    public bool isMainQuest;

    public List<QuestGoal> goals = new();
    public List<QuestReward> rewards = new();
}

public class QuestStringData
{
    public int QuestStringID;
    public string TextKR;
}
