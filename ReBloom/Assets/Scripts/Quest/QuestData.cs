using System.Collections.Generic;
using System;

public enum QuestGoalType
{
    None = 0,
    Collect = 1,   
    Craft = 2,
}

[Serializable]
public class QuestGoal
{
    public QuestGoalType type;
    public int objectId;
    public int amount;
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
    public int formerQuestId;   

    public List<QuestGoal> goals = new();
    public List<QuestReward> rewards = new();
}
