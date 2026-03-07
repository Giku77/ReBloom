using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class QuestManagerSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "quest_manager";

    public void Capture(SaveGameDTO save)
    {
        if (save == null)
            return;

        if (TryCaptureNetworkQuest(save))
            return;

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

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            await UniTask.WaitUntil(() => NetworkQuestManager.I != null && NetworkQuestManager.I.IsSpawned);
            NetworkQuestManager.I.RestoreStateFromSave(save.quest);
            return;
        }

        QuestManager.I.SetFirstQuest(save.quest.firstQuestCompleted);
        int id = save.quest.currentQuestId;
        if (id > 0) QuestManager.I.SetCurrent(id);
        if (QuestManager.I.Current != null) QuestManager.I.PlayQuestCompleteAnimation();
    }

    private static bool TryCaptureNetworkQuest(SaveGameDTO save)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return false;

        var nqm = NetworkQuestManager.I;
        if (nqm == null)
            return false;

        save.quest.currentQuestId = nqm.CurrentQuestId;
        save.quest.firstQuestCompleted = nqm.FirstQuestCompleted;
        return true;
    }
}
