using UnityEngine;

public class TutorialSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "tutorial";

    public void Capture(SaveGameDTO save)
    {
        if (TutorialManager.I == null) return;

        save.tutorial.introCompleted = TutorialManager.I.IntroCompleted;
        save.tutorial.resumeTutorialId = TutorialManager.I.ResumeTutorialId;
    }

    public void Restore(SaveGameDTO save)
    {
        if (TutorialManager.I == null) return;

        TutorialManager.I.SetTutorialState(save.tutorial.resumeTutorialId, save.tutorial.introCompleted);
    }
}
