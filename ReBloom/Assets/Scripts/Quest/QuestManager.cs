using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    private QuestDB _db;
    private QuestData _current;
    private GameInventory _inventory;
    public GameInventory Inventory => _inventory;

    private StageDetector _stageDetector;
    public QuestData Current => _current;
    public QuestDB DB => _db;

    //채집 오브젝트 첫번째 퀘스트 후 삭제 관련 필드
    public static event Action OnFirstQuestCompleted;
    private int _firstQuestId;
    private bool _firstQuestCompleted;
    public bool FirstQuestCompleted => _firstQuestCompleted;
    public int FirstQuestId => _firstQuestId;


    [SerializeField] private CutSceneManager cutSceneManager;
    [SerializeField] private GreeningVisualController greeningVisualController;

    [SerializeField] private int endingCutSceneStartId = 1301021; // CutScene10 시작
    private bool endingPlayed;



    private void Awake() => I = this;

    [SerializeField] private QuestTextSwitcher questTextSwitcher;
    [SerializeField] private QuestUI questUI;

    public event Action OnQuestStateChanged;

    private void RaiseQuestChanged()
    {
        OnQuestStateChanged?.Invoke();
        PlayQuestCompleteAnimation();
    }

    public void TryAdvanceOrPlayEnding()
    {
        if (endingPlayed) return;

        bool isEndingReady = ResearchManager.I != null && ResearchManager.I.CurrentGreening >= 100f;
        if (!isEndingReady) return;

        if (_current != null)
        {
            int nextId = FindNextByFormer(_current.questId);
            if (nextId != 0) return; 
        }

        PlayEndingSequence().Forget();
    }

    private async UniTaskVoid PlayEndingSequence()
    {
        endingPlayed = true;

        if (greeningVisualController != null)
            greeningVisualController.ForceDisableInGameFog();

        if (cutSceneManager != null)
            await cutSceneManager.PlayCutSceneSequenceAsync(endingCutSceneStartId, false);

        AutoSaveService.I?.RequestSave("EndingCutScenePlayed");
    }

    public void SetFirstQuest(bool completed)
    {
        _firstQuestCompleted = completed;
        if (completed)
        {
            OnFirstQuestCompleted?.Invoke();
        }
    }


    public void Init(QuestDB db, GameInventory inventory, StageDetector stageDetector)
    {
        if (_inventory != null)
        {
            _inventory.OnInventoryBound -= BindInventoryEvents;
            if (_inventory.Container != null)
                _inventory.Container.OnContainerChanged -= HandleInventoryChanged;
        }

        _db = db;
        _inventory = inventory;
        _stageDetector = stageDetector;

        if (_inventory != null)
        {
            _inventory.OnInventoryBound += BindInventoryEvents;
            BindInventoryEvents(); // 이미 바인딩된 상태면 즉시 연결
        }

        foreach (var kv in db.GetAll())
        {
            if (kv.Value.formerQuestId == 0)
            {
                _firstQuestId = kv.Value.questId;
                SetCurrent(kv.Value.questId);
                break;
            }
        }

        if (ResearchManager.I != null)
            ResearchManager.I.OnGreeningChanged += HandleGreeningChanged;

        SyncEndingGoalsWithWorld(_current);
    }

    private void BindInventoryEvents()
    {
        if (_inventory == null || _inventory.Container == null)
            return;

        _inventory.Container.OnContainerChanged -= HandleInventoryChanged;
        _inventory.Container.OnContainerChanged += HandleInventoryChanged;

        Debug.Log("[QuestManager] Inventory OnContainerChanged 연결 완료");
    }

    private void HandleInventoryChanged()
    {
        RaiseQuestChanged();
    }

    private void HandleGreeningChanged(float value)
    {
        if (_current == null || _current.goals == null) return;

        bool changed = SyncEndingGoalsWithWorld(_current);

        if (changed)
        {
            RaiseQuestChanged();
            if (IsQuestSatisfied(_current))
                PlayQuestCompleteAnimation();
        }
    }

    private bool SyncEndingGoalsWithWorld(QuestData quest)
    {
        if (quest == null || quest.goals == null) return false;
        if (ResearchManager.I == null) return false;

        bool changed = false;
        int greeningInt = Mathf.FloorToInt(ResearchManager.I.CurrentGreening);

        foreach (var g in quest.goals)
        {
            if (g == null) continue;
            if (g.type != QuestGoalType.Ending) continue;

            int prev = g.currentCount;
            g.currentCount = greeningInt;  
            if (prev != g.currentCount) changed = true;
        }

        return changed;
    }


    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnInventoryBound -= BindInventoryEvents;

            if (_inventory.Container != null)
                _inventory.Container.OnContainerChanged -= HandleInventoryChanged;
        }

        if (ResearchManager.I != null)
            ResearchManager.I.OnGreeningChanged -= HandleGreeningChanged;

        if (I == this) I = null;
    }

    public void SetCurrent(int questId)
    {
        if (!_db.TryGet(questId, out var data))
        {
            Debug.LogError($"퀘스트 DB에 ID {questId}가 없습니다.");
            return;
        }

        _current = data;

        SyncEndingGoalsWithWorld(_current);
        SyncBuildGoalsWithWorld(_current);

        questUI?.SetShowPathGuide(false);
        questUI?.ClearPathGuide(); 
        RaiseQuestChanged();
        //questUI?.Refresh();
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

    public void NotifyInteracted(int interactedObjectId)
    {
        if (_current == null || _current.goals == null) return;

        bool changed = false;

        foreach (var g in _current.goals)
        {
            if (g == null) continue;

            if (g.type == QuestGoalType.Interact && g.objectId == interactedObjectId)
            {
                g.currentCount = Mathf.Clamp(g.currentCount + 1, 0, g.amount);
                changed = true;
            }
        }

        if (changed)
        {
            RaiseQuestChanged();

            if (IsQuestSatisfied(_current))
                PlayQuestCompleteAnimation();
        }
    }

    public void DebugForceCompleteAndGoNext()
    {
        if (_current == null) return;

        int completedQuestId = _current.questId;

        TutorialEventBus.RaiseTarget(completedQuestId);

        if (completedQuestId == _firstQuestId)
        {
            _firstQuestCompleted = true;
            OnFirstQuestCompleted?.Invoke();
        }

        var nextId = FindNextByFormer(completedQuestId);
        if (nextId == 0)
        {
            _current = null;
            Debug.Log("[Quest] Force complete: next quest 없음");
        }
        else
        {
            SetCurrent(nextId);
            questTextSwitcher?.ResetQuestText();
            Debug.Log($"[Quest] Force complete: {completedQuestId} -> {nextId}");
        }

        AutoSaveService.I?.RequestSave("QuestProgress");
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

        SoundManager.I?.PlayMissionClear();
        questTextSwitcher?.PlayQuestComplete();
    }

    public void TryCompleteCurrent()
    {
        if (questTextSwitcher != null && questTextSwitcher.IsAnimating()) return;

        if (_current == null) return;

        SoundManager.I?.PlayNextMission();
        //if (!IsQuestSatisfied(_current))
        //{
        //    Debug.Log($"퀘스트 조건 미달성 : {_current.questName}");
        //    return;
        //}

        int completedQuestId = _current.questId;
        TutorialEventBus.RaiseTarget(completedQuestId);

        if (completedQuestId == _firstQuestId)
        {
            _firstQuestCompleted = true;
            OnFirstQuestCompleted?.Invoke();
        }

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
        AutoSaveService.I?.RequestSave("QuestProgress");
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

    public void PlayQuestCompleteAnimationForced()
    {
        SoundManager.I?.PlayMissionClear();
        questTextSwitcher?.PlayQuestComplete();
    }

    public void HideQuestCompleteAnimation()
    {
        questTextSwitcher?.ResetQuestText();
    }
}
