using BansheeGz.BGDatabase;
using System.Collections.Generic;

public class TutorialDB
{
    private readonly Dictionary<int, TutorialData> _tutorials = new();
    private readonly Dictionary<int, TutorialStringData> _strings = new();

    public void LoadFromBG()
    {
        var tutorialMeta = BGRepo.I.GetMeta("Tutorial");

        foreach (var e in tutorialMeta.EntitiesToList())
        {
            var d = new TutorialData
            {
                TutorialID       = e.Get<int>("TutorialID"),
                TextType         = (TutorialTextType)e.Get<int>("TutorialTextType"),
                TutorialTextID   = e.Get<int>("TutorialTextID"),
                NextTutorialID   = e.Get<int>("NextTutorial"),
                Condition        = (TutorialConditionType)e.Get<int>("TutorialCondition"),
                ConditionObjectID= e.Get<int>("ConditionObjectID"),
                IsControllable   = e.Get<bool>("isControllable")
            };

            _tutorials[d.TutorialID] = d;
        }

        var stringMeta = BGRepo.I.GetMeta("Tutorial_String");

        foreach (var e in stringMeta.EntitiesToList())
        {
            var s = new TutorialStringData
            {
                TutorialStringID = e.Get<int>("TutorialStringID"),
                TextKR           = e.Get<string>("TutorialStringKR")
            };

            _strings[s.TutorialStringID] = s;
        }
    }

    public bool TryGetTutorial(int tutorialId, out TutorialData data)
        => _tutorials.TryGetValue(tutorialId, out data);

    public bool TryGetString(int stringId, out TutorialStringData data)
        => _strings.TryGetValue(stringId, out data);

    public string GetTextKR(int stringId)
        => _strings.TryGetValue(stringId, out var d) ? d.TextKR : $"#{stringId}";

    public IReadOnlyDictionary<int, TutorialData> GetAllTutorials() => _tutorials;
    public IReadOnlyDictionary<int, TutorialStringData> GetAllStrings() => _strings;
}
