using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    private QuestDB _db;
    private QuestData _current;
    private GameInventory _inventory;
    private StageDetector _stageDetector;
    private readonly List<IQuestGoal> _runtimeGoals = new();

    public GameInventory Inventory => _inventory;
    public QuestData Current => _current;
    public QuestDB DB => _db;
    public bool IsInitialized => _db != null && _inventory != null && _stageDetector != null;

    public static event Action OnFirstQuestCompleted;

    private int _firstQuestId;
    private bool _firstQuestCompleted;
    private bool _currentQuestSatisfied;

    public bool FirstQuestCompleted => _firstQuestCompleted;
    public int FirstQuestId => _firstQuestId;
    public bool EndingPlayed => endingPlayed;

    [SerializeField] private CutSceneManager cutSceneManager;
    [SerializeField] private GreeningVisualController greeningVisualController;
    [SerializeField] private int endingCutSceneStartId = 1301021;
    [SerializeField] private QuestTextSwitcher questTextSwitcher;
    [SerializeField] private QuestUI questUI;

    private bool endingPlayed;

    public event Action OnQuestStateChanged;

    private void Awake() => I = this;

    public void Init(QuestDB db, GameInventory inventory, StageDetector stageDetector)
    {
        UnsubscribeWorldEvents();

        _db = db;
        _inventory = inventory;
        _stageDetector = stageDetector;

        SubscribeWorldEvents();

        _firstQuestId = FindFirstQuestId();
        if (_current == null && _firstQuestId != 0)
            SetCurrent(_firstQuestId, null, suppressCompleteFx: true);
    }

    public void SetFirstQuest(bool completed)
    {
        _firstQuestCompleted = completed;
        if (completed)
            OnFirstQuestCompleted?.Invoke();
    }

    public void RestoreFromSave(QuestSaveDTO questSave)
    {
        SetFirstQuest(questSave.firstQuestCompleted);
        endingPlayed = questSave.endingPlayed;

        if (questSave.currentQuestId > 0)
        {
            SetCurrent(questSave.currentQuestId, questSave.goalProgress, suppressCompleteFx: true);
            return;
        }

        ClearCurrentQuest();
    }

    public List<QuestGoalProgressDTO> CaptureGoalProgress()
    {
        var result = new List<QuestGoalProgressDTO>();
        foreach (var goal in _runtimeGoals)
            result.Add(goal.CaptureProgress());
        return result;
    }

    public void SetCurrent(int questId)
    {
        SetCurrent(questId, null, suppressCompleteFx: false);
    }

    public void TryAdvanceOrPlayEnding()
    {
        if (endingPlayed)
            return;

        bool isEndingReady = ResearchManager.I != null && ResearchManager.I.CurrentGreening >= 100f;
        if (!isEndingReady)
            return;

        if (_current != null)
        {
            if (!AreCurrentGoalsSatisfied())
                return;

            int nextId = FindNextByFormer(_current.questId);
            if (nextId != 0)
                return;
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

    public void NotifyBuildingBuilt(int buildingId)
    {
        ProcessEvent(QuestEvent.BuildingChanged(buildingId));
    }

    public void NotifyInteracted(int interactedObjectId)
    {
        ProcessEvent(QuestEvent.Interacted(interactedObjectId));
    }

    public void DebugForceCompleteAndGoNext()
    {
        if (_current == null)
            return;

        AdvanceCurrentQuest(force: true, grantRewards: false);
        Debug.Log("[Quest] Force complete current quest");
    }

    public void ClearPathGuide()
    {
        questUI?.ClearPathGuide();
    }

    public void PlayQuestCompleteAnimation()
    {
        if (_current == null)
            return;
        if (!AreCurrentGoalsSatisfied())
            return;

        SoundManager.I?.PlayMissionClear();
        questTextSwitcher?.PlayQuestComplete();
    }

    public void TryCompleteCurrent()
    {
        if (questTextSwitcher != null && questTextSwitcher.IsAnimating())
            return;
        if (_current == null)
            return;
        if (!AreCurrentGoalsSatisfied())
        {
            Debug.Log($"퀘스트 조건 미달성 : {_current.questName}");
            return;
        }

        SoundManager.I?.PlayNextMission();
        AdvanceCurrentQuest(force: false, grantRewards: true);
    }

    public void CompleteCurrent()
    {
        if (_current == null)
            return;

        AdvanceCurrentQuest(force: true, grantRewards: true);
    }

    private void SetCurrent(int questId, List<QuestGoalProgressDTO> savedProgress, bool suppressCompleteFx)
    {
        if (_db == null || !_db.TryGet(questId, out var data))
        {
            Debug.LogError($"퀘스트 DB에 ID {questId}가 없습니다.");
            return;
        }

        _current = data;
        RebuildRuntimeGoals(savedProgress);

        questUI?.SetShowPathGuide(false);
        questUI?.ClearPathGuide();
        RaiseQuestChanged(!suppressCompleteFx);
    }

    private void RebuildRuntimeGoals(List<QuestGoalProgressDTO> savedProgress)
    {
        _runtimeGoals.Clear();
        if (_current?.goals == null)
        {
            _currentQuestSatisfied = _current == null;
            return;
        }

        _runtimeGoals.AddRange(QuestGoalFactory.CreateGoals(_current.goals));

        foreach (var goal in _runtimeGoals)
        {
            var progress = FindSavedProgress(savedProgress, goal);
            goal.RestoreProgress(progress);
        }

        SyncCurrentGoals();
        _currentQuestSatisfied = AreCurrentGoalsSatisfied();
    }

    private QuestGoalProgressDTO FindSavedProgress(List<QuestGoalProgressDTO> savedProgress, IQuestGoal goal)
    {
        if (savedProgress == null)
            return null;

        foreach (var progress in savedProgress)
        {
            if (progress == null)
                continue;
            if (progress.goalIndex == goal.GoalIndex)
                return progress;
        }

        return null;
    }

    private bool SyncCurrentGoals()
    {
        if (_runtimeGoals.Count == 0)
            return false;

        bool changed = false;
        var context = BuildGoalContext();
        foreach (var goal in _runtimeGoals)
            changed |= goal.Sync(context);
        return changed;
    }

    private QuestGoalContext BuildGoalContext()
    {
        return new QuestGoalContext(_inventory, _stageDetector);
    }

    private bool ProcessEvent(in QuestEvent questEvent)
    {
        if (_current == null || _runtimeGoals.Count == 0)
            return false;

        bool changed = false;
        var context = BuildGoalContext();
        foreach (var goal in _runtimeGoals)
            changed |= goal.HandleEvent(questEvent, context);

        if (changed)
            RaiseQuestChanged();

        return changed;
    }

    private void RaiseQuestChanged(bool allowCompleteAnimation = true)
    {
        bool satisfied = AreCurrentGoalsSatisfied();
        OnQuestStateChanged?.Invoke();

        if (allowCompleteAnimation && satisfied && !_currentQuestSatisfied)
            PlayQuestCompleteAnimation();

        _currentQuestSatisfied = satisfied;
    }

    private bool AreCurrentGoalsSatisfied()
    {
        if (_current == null)
            return false;
        if (_runtimeGoals.Count == 0)
            return true;

        foreach (var goal in _runtimeGoals)
        {
            if (goal == null)
                continue;
            if (!goal.IsSatisfied())
                return false;
        }

        return true;
    }

    private void AdvanceCurrentQuest(bool force, bool grantRewards)
    {
        if (_current == null)
            return;
        if (!force && !AreCurrentGoalsSatisfied())
            return;

        int completedQuestId = _current.questId;

        if (grantRewards)
            GrantRewards(_current);

        TutorialEventBus.RaiseTarget(completedQuestId);

        if (completedQuestId == _firstQuestId)
            SetFirstQuest(true);

        int nextId = FindNextByFormer(completedQuestId);
        if (nextId == 0)
        {
            ClearCurrentQuest();
            Debug.Log("퀘스트 완료, 다음 퀘스트 없음.");
        }
        else
        {
            SetCurrent(nextId);
            questTextSwitcher?.ResetQuestText();
        }

        AutoSaveService.I?.RequestSave("QuestProgress");
        if (questUI != null && questUI.GetShowPathGuide())
            questUI.SetShowPathGuide(false);
    }

    private void GrantRewards(QuestData quest)
    {
        if (quest?.rewards == null || _inventory == null)
            return;

        foreach (var reward in quest.rewards)
        {
            if (reward == null || reward.itemId == 0 || reward.amount <= 0)
                continue;

            _inventory.AddItemFromWorld(reward.itemId, reward.amount, drop: true);
        }
    }

    private void ClearCurrentQuest()
    {
        _current = null;
        _runtimeGoals.Clear();
        _currentQuestSatisfied = false;
        questUI?.SetShowPathGuide(false);
        questUI?.ClearPathGuide();
        OnQuestStateChanged?.Invoke();
    }

    private void SubscribeWorldEvents()
    {
        if (_inventory?.Container != null)
            _inventory.Container.OnContainerChanged += HandleInventoryChanged;

        if (ResearchManager.I != null)
            ResearchManager.I.OnGreeningChanged += HandleGreeningChanged;

        StageDetector.OnStageChanged += HandleStageChanged;
        BuildManager.OnBuildingChanged += HandleBuildingChanged;
    }

    private void UnsubscribeWorldEvents()
    {
        if (_inventory?.Container != null)
            _inventory.Container.OnContainerChanged -= HandleInventoryChanged;

        if (ResearchManager.I != null)
            ResearchManager.I.OnGreeningChanged -= HandleGreeningChanged;

        StageDetector.OnStageChanged -= HandleStageChanged;
        BuildManager.OnBuildingChanged -= HandleBuildingChanged;
    }

    private void HandleInventoryChanged()
    {
        ProcessEvent(QuestEvent.InventoryChanged());
    }

    private void HandleGreeningChanged(float value)
    {
        ProcessEvent(QuestEvent.GreeningChanged(value));
        TryAdvanceOrPlayEnding();
    }

    private void HandleStageChanged(int stageId)
    {
        ProcessEvent(QuestEvent.StageChanged(stageId));
    }

    private void HandleBuildingChanged(int buildingId)
    {
        ProcessEvent(QuestEvent.BuildingChanged(buildingId));
    }

    private int FindFirstQuestId()
    {
        if (_db == null)
            return 0;

        foreach (var kv in _db.GetAll())
        {
            if (kv.Value.formerQuestId == 0)
                return kv.Value.questId;
        }

        return 0;
    }

    private int FindNextByFormer(int formerId)
    {
        if (_db == null)
            return 0;

        foreach (var q in _db.GetAll())
        {
            if (q.Value.formerQuestId == formerId)
                return q.Value.questId;
        }

        return 0;
    }

    private void OnDestroy()
    {
        UnsubscribeWorldEvents();

        if (I == this)
            I = null;
    }
}

