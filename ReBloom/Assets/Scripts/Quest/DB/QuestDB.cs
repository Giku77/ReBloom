
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using UnityEngine;

public class QuestDB
{
    private Dictionary<int, QuestData> _byId = new();
    private Dictionary<int, QuestStringData> _strings = new();

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
        var stringMeta = BGRepo.I.GetMeta("Quest_String");
        foreach (var e in stringMeta.EntitiesToList())
        {
            var s = new QuestStringData
            {
                QuestStringID = e.Get<int>("stringID"),
                TextKR        = e.Get<string>("stringKOR")
            };

            _strings[s.QuestStringID] = s;
        }
    }

    public bool TryGet(int questId, out QuestData data)
        => _byId.TryGetValue(questId, out data);
    public bool TryGetString(int stringId, out QuestStringData data)
        => _strings.TryGetValue(stringId, out data);
    public string GetTextKR(int stringId)
    {
        if (!_strings.TryGetValue(stringId, out var data) || string.IsNullOrWhiteSpace(data.TextKR))
        {
            Debug.LogWarning($"[QuestDB] Missing or empty Quest_String row. stringId={stringId}");
            return $"#{stringId}";
        }

        return data.TextKR;
    }


    private QuestData ParseQuest(BGEntity entity)
    {
        var q = new QuestData();
        q.questId = entity.Get<int>("questID");
        q.questName = entity.Get<string>("questName");
        q.questNameID = entity.Get<int>("questNameID");
        q.questTextID = entity.Get<int>("questTextID");
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
