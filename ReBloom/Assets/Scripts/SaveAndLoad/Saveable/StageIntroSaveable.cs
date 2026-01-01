using System.Collections.Generic;
using UnityEngine;

public class StageIntroSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private StageIntroDirector director;

    public string EntityGuid => "stageIntroSaveable";

    public void Capture(SaveGameDTO save)
    {
        if (director == null) return;
        save.world.visitedStages = new List<int>(director.CaptureVisited());
    }

    public void Restore(SaveGameDTO save)
    {
        if (director == null) return;
        director.ApplyVisited(save.world.visitedStages);
    }
}
