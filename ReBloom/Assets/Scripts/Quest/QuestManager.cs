using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    private QuestDB _db;
    private QuestData _current;
    private IInventoryProvider _inventory;
    public IInventoryProvider Inventory => _inventory;

    private StageDetector _stageDetector;
    public QuestData Current => _current;

    private void Awake() => I = this;

    public void Init(QuestDB db, IInventoryProvider inventory, StageDetector stageDetector)
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

        var ui = FindFirstObjectByType<QuestUI>();
        ui?.Refresh();
    }

    public void NotifyBuildingBuilt(int buildingId)
    {
        if (_current == null) return;
        if (_current.goals == null) return;

        bool changed = false;

        foreach (var g in _current.goals)
        {
            if (g.type == QuestGoalType.Craft && buildingId == g.objectId)
            {
                g.currentCount++;
                changed = true;
            }
        }

        if (changed)
        {
            TryCompleteCurrent();
        }
    }

    public void TryCompleteCurrent()
    {
        if (_current == null) return;

        if (!IsQuestSatisfied(_current))
        {
            Debug.Log("조건이 아직 안 됨");
            return;
        }

        var nextId = FindNextByFormer(_current.questId);
        if (nextId == 0)
        {
            _current = null;
            Debug.Log("모든 퀘스트 완료");
        }
        else
        {
            SetCurrent(nextId);
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
