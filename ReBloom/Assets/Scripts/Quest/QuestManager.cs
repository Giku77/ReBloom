using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    private QuestDB _db;
    private QuestData _current;
    public QuestData Current => _current;

    private void Awake() => I = this;

    public void Init(QuestDB db)
    {
        _db = db;

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
            Debug.LogError($"¾ø´Â Äù½ºÆ® {questId}");
            return;
        }

        _current = data;
        
        var ui = FindFirstObjectByType<QuestUI>();
        ui?.Refresh();
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
