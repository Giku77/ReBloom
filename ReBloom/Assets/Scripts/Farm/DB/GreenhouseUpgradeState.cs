using System;
using System.Collections.Generic;

[Serializable]
public class GreenhouseUpgradeState
{
    [Serializable]
    public struct SortProgress
    {
        public int sort;
        public int completedGrade;
    }

    public string greenhouseId;
    public List<SortProgress> progress = new();

    public int GetCompletedGrade(int sort)
    {
        for (int i = 0; i < progress.Count; i++)
            if (progress[i].sort == sort)
                return progress[i].completedGrade;
        return 0;
    }

    public void SetCompletedGrade(int sort, int grade)
    {
        for (int i = 0; i < progress.Count; i++)
        {
            if (progress[i].sort == sort)
            {
                progress[i] = new SortProgress { sort = sort, completedGrade = grade };
                return;
            }
        }
        progress.Add(new SortProgress { sort = sort, completedGrade = grade });
    }
}
