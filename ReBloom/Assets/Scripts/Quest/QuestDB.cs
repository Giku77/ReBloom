
using BansheeGz.BGDatabase;
using System.Collections.Generic;

public class QuestDB
{
    private Dictionary<int, QuestData> _byId = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Quest");
        if (meta == null)
        {
            return;
        }

        _byId.Clear();
        foreach (var entity in meta.EntitiesToList())
        {
            var q = ParseQuest(entity);
            _byId[q.questId] = q;
        }
    }

    public bool TryGet(int questId, out QuestData data)
        => _byId.TryGetValue(questId, out data);


    private QuestData ParseQuest(BGEntity entity)
    {
        var q = new QuestData();
        q.questId = entity.Get<int>("questID");
        q.questName = entity.Get<string>("questName");
        q.formerQuestId = entity.Get<int>("formerQuestID");
        q.isMainQuest = entity.Get<bool>("isMainQuest");

        for (int i = 1; i <= 3; i++)
        {
            var type = entity.Get<int>($"goal{i}Type");
            if (type == 0) continue;  

            var goal = new QuestGoal
            {
                type = (QuestGoalType)type,
                objectId = entity.Get<int>($"goal{i}ObjectID"),
                amount = entity.Get<int>($"goal{i}ObjectAmount"),
            };
            q.goals.Add(goal);
        }

        for (int i = 1; i <= 3; i++)
        {
            var id = entity.Get<int>($"reward{i}ID");
            var amt = entity.Get<int>($"reward{i}Amount");
            if (id == 0) continue;

            q.rewards.Add(new QuestReward
            {
                itemId = id,
                amount = amt
            });
        }

        return q;
    }

    public Dictionary<int, QuestData> GetAll()
        => _byId;
}
