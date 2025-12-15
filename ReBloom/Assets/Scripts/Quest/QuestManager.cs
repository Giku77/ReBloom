using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    private QuestDB _db;
    private QuestData _current;
    private IGameInventory _inventory;
    public IGameInventory Inventory => _inventory;

    private StageDetector _stageDetector;
    public QuestData Current => _current;
    public QuestDB DB => _db;


    private void Awake() => I = this;

    [SerializeField] private QuestTextSwitcher questTextSwitcher;
    [SerializeField] private QuestUI questUI;



    public void Init(QuestDB db, IGameInventory inventory, StageDetector stageDetector)
    {
        _db = db;
        _inventory = inventory;
        _stageDetector = stageDetector;

        foreach (var kv in db.GetAll())   
        {
            if (kv.Value.formerQuestId == 0)
            {
                SetCurrent(kv.Value.questId);
                break;
            }
        }
    }

    public void SetCurrent(int questId)
    {
        if (!_db.TryGet(questId, out var data))
        {
            Debug.LogError($"퀘스트 DB에 ID {questId}가 없습니다.");
            return;
        }

        _current = data;

        SyncBuildGoalsWithWorld(_current);

        questUI?.Refresh();
    }

    private void SyncBuildGoalsWithWorld(QuestData quest)
    {
        if (quest.goals == null) return;
        if (BuildManager.I == null) return;

        foreach (var g in quest.goals)
        {
            if (g == null) continue;

            if (g.type == QuestGoalType.Craft) 
            {
                int count = BuildManager.I.GetCount(g.objectId);
                g.currentCount = count; 
            }
        }
    }

    public void NotifyBuildingBuilt(int buildingId)
    {
        if (_current == null) return;
        if (_current.goals == null) return;
        if (BuildManager.I == null) return;

        bool changed = false;

        foreach (var g in _current.goals)
        {
            if (g == null) continue;

            if (g.type == QuestGoalType.Craft && buildingId == g.objectId)
            {
                int count = BuildManager.I.GetCount(g.objectId);
                g.currentCount = count;      
                changed = true;
            }
        }

        if (changed)
        {
            PlayQuestCompleteAnimation();
        }
    }

    public void ClearPathGuide()
    {
        questUI?.ClearPathGuide();
    }

    public void PlayQuestCompleteAnimation()
    {
        if (_current == null) return;

        if (!IsQuestSatisfied(_current))
        {
            Debug.Log($"퀘스트 조건 미달성 : {_current.questName}");
            return;
        }

        questTextSwitcher?.PlayQuestComplete();
    }

    public void TryCompleteCurrent()
    {
        if (questTextSwitcher != null && questTextSwitcher.IsAnimating()) return;

        if (_current == null) return;

        //if (!IsQuestSatisfied(_current))
        //{
        //    Debug.Log($"퀘스트 조건 미달성 : {_current.questName}");
        //    return;
        //}

        int completedQuestId = _current.questId;
        TutorialEventBus.RaiseTarget(completedQuestId);

        var nextId = FindNextByFormer(_current.questId);
        if (nextId == 0)
        {
            _current = null;
            Debug.Log("퀘스트 완료, 다음 퀘스트 없음.");
        }
        else
        {
            SetCurrent(nextId);
            questTextSwitcher?.ResetQuestText();
            if (IsQuestSatisfied(_current))
            {
                PlayQuestCompleteAnimation();
            }
        }
        if (questUI != null && questUI.GetShowPathGuide())
        {
              //questUI.TargetIndex = Mathf.Clamp(questUI.TargetIndex + 1, 0, questUI.GetPathTransformCount() - 1);
              questUI?.SetShowPathGuide(false);
        }
    }
    
    bool IsQuestSatisfied(QuestData data)
    {
        if (data.goals == null || data.goals.Count == 0)
            return true;

        foreach (var g in data.goals)
        {
            if (g == null) continue;
            if (!g.IsSatisfied(_inventory, _stageDetector))
                return false;
        }
        return true;
    }

    public void CompleteCurrent()
    {
        if (_current == null) return;

        var nextId = FindNextByFormer(_current.questId);
        if (nextId == 0)
        {
            _current = null;
        }
        else
        {
            SetCurrent(nextId);
        }
    }

    private int FindNextByFormer(int formerId)
    {
        foreach (var q in _db.GetAll())
        {
            if (q.Value.formerQuestId == formerId)
                return q.Value.questId;
        }
        return 0;
    }
}
