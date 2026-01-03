using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuestManagerSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "quest_manager";

    public void Capture(SaveGameDTO save)
    {
        save.quest.currentQuestId = QuestManager.I != null && QuestManager.I.Current != null
            ? QuestManager.I.Current.questId
            : 0;
        save.quest.firstQuestCompleted = QuestManager.I != null
            ? QuestManager.I.FirstQuestCompleted
            : false;
    }
    public void Restore(SaveGameDTO save)
    {
        RestoreAsync(save).Forget();
    }

    private async UniTaskVoid RestoreAsync(SaveGameDTO save)
    {
        await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null);
        await UniTask.DelayFrame(1);

        QuestManager.I.SetFirstQuest(save.quest.firstQuestCompleted);
        int id = save.quest.currentQuestId;
        if (id > 0) QuestManager.I.SetCurrent(id);
        if (QuestManager.I.Current != null) QuestManager.I.PlayQuestCompleteAnimation();
        
    }
}
