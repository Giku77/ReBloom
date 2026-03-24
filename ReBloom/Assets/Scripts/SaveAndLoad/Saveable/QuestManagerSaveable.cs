using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuestManagerSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "quest_manager";

    public void Capture(SaveGameDTO save)
    {
        if (QuestManager.I == null)
            return;

        save.quest.currentQuestId = QuestManager.I.Current != null
            ? QuestManager.I.Current.questId
            : 0;
        save.quest.firstQuestCompleted = QuestManager.I.FirstQuestCompleted;
        save.quest.endingPlayed = QuestManager.I.EndingPlayed;
        save.quest.goalProgress = QuestManager.I.CaptureGoalProgress();
    }

    public void Restore(SaveGameDTO save)
    {
        RestoreAsync(save).Forget();
    }

    private async UniTaskVoid RestoreAsync(SaveGameDTO save)
    {
        await UniTask.WaitUntil(() => QuestManager.I != null && QuestManager.I.IsInitialized);
        await UniTask.DelayFrame(1);

        QuestManager.I.RestoreFromSave(save.quest);
    }
}
