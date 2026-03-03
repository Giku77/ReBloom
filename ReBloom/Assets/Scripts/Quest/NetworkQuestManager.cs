using Cysharp.Threading.Tasks;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkQuestManager : NetworkBehaviour
{
    public static NetworkQuestManager I { get; private set; }

    [SerializeField] private QuestDB questDB;

    private NetworkVariable<int> currentQuestId = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<QuestGoalProgressState> goalStates;

    public QuestDB QuestDB => questDB;

    public event Action OnQuestUpdated;

    private void Awake()
    {
        I = this;
        goalStates = new NetworkList<QuestGoalProgressState>();

        if (questDB == null)
        {
            questDB = new QuestDB();
            questDB.LoadFromBG();
            Debug.Log("[NQM] QuestDB 직접 로드 완료");
        }
    }

    public override void OnDestroy()
    {
        if (I == this) I = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[NQM] OnNetworkSpawn IsServer={IsServer} IsClient={IsClient} IsSpawned={IsSpawned} questDB={(questDB != null)} currentQuestId={currentQuestId.Value}");
        currentQuestId.OnValueChanged += OnQuestIdChanged;
        goalStates.OnListChanged += OnGoalStatesChanged;

        if (IsServer && currentQuestId.Value == 0)
            InitServerDeferred().Forget();

        OnQuestUpdated?.Invoke();
    }

    private async UniTaskVoid InitServerDeferred()
    {
        await UniTask.WaitUntil(() => questDB != null);
        await UniTask.DelayFrame(1);

        await UniTask.WaitUntil(() => questDB.GetAll() != null);

        InitializeFirstQuestServer();
    }

    public override void OnNetworkDespawn()
    {
        currentQuestId.OnValueChanged -= OnQuestIdChanged;
        goalStates.OnListChanged -= OnGoalStatesChanged;
    }

    private void OnQuestIdChanged(int oldValue, int newValue)
    {
        OnQuestUpdated?.Invoke();
    }

    public void ReportInteract(int objectId, int amount = 1)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
            AddInteractProgressServer(objectId, amount);
        else
            ReportInteractRpc(objectId, amount);
    }

    public void ReportCollect(int itemId, int amount)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
            AddCollectProgressServer(itemId, amount);
        else
            ReportCollectRpc(itemId, amount);
    }

    public void ReportCraft(int objectId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            int count = BuildManager.I != null ? BuildManager.I.GetCount(objectId) : 0;
            SetCraftProgressServer(objectId, count);
        }
        else
        {
            ReportCraftRpc(objectId);
        }
    }

    [Rpc(SendTo.Server)]
    private void ReportInteractRpc(int objectId, int amount, RpcParams rpcParams = default)
    {
        AddInteractProgressServer(objectId, amount);
    }

    [Rpc(SendTo.Server)]
    private void ReportCollectRpc(int itemId, int amount, RpcParams rpcParams = default)
    {
        AddCollectProgressServer(itemId, amount);
    }

    [Rpc(SendTo.Server)]
    private void ReportCraftRpc(int objectId, RpcParams rpcParams = default)
    {
        int count = BuildManager.I != null ? BuildManager.I.GetCount(objectId) : 0;
        SetCraftProgressServer(objectId, count);
    }

    private void OnGoalStatesChanged(NetworkListEvent<QuestGoalProgressState> changeEvent)
    {
        OnQuestUpdated?.Invoke();
    }

    private void InitializeFirstQuestServer()
    {
        if (questDB == null)
        {
            Debug.LogError("[NetworkQuestManager] questDB가 null");
            return;
        }

        foreach (var kv in questDB.GetAll())
        {
            var q = kv.Value;
            if (q == null)
            {
                Debug.LogWarning($"[NetworkQuestManager] questDB에 null QuestData 엔트리 있음. key={kv.Key}");
                continue;
            }

            if (q.formerQuestId == 0)
            {
                SetCurrentQuestServer(q.questId);
                return;
            }
        }

        Debug.LogError("[NetworkQuestManager] formerQuestId==0 인 첫 퀘스트를 찾지 못함");
    }

    public void SetCurrentQuestServer(int questId)
    {
        Debug.Log($"[NQM] SetCurrentQuestServer called. IsServer={NetworkManager.Singleton?.IsServer} questId={questId}");
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (!questDB.TryGet(questId, out var quest))
            return;

        currentQuestId.Value = questId;
        goalStates.Clear();

        if (quest.goals == null) return;

        for (int i = 0; i < quest.goals.Count; i++)
        {
            var g = quest.goals[i];
            if (g == null) continue;

            goalStates.Add(new QuestGoalProgressState
            {
                goalIndex = i,
                objectId = g.objectId,
                currentCount = g.currentCount,
                targetCount = g.amount
            });
        }
    }

    public int CurrentQuestId => currentQuestId.Value;

    public QuestData GetCurrentQuest()
    {
        if (questDB == null || currentQuestId.Value == 0) return null;
        questDB.TryGet(currentQuestId.Value, out var q);
        return q;
    }

    public int GetGoalCurrentCount(int goalIndex)
    {
        for (int i = 0; i < goalStates.Count; i++)
        {
            if (goalStates[i].goalIndex == goalIndex)
                return goalStates[i].currentCount;
        }
        return 0;
    }

    public void AddCollectProgressServer(int objectId, int amount)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        var quest = GetCurrentQuest();
        if (quest == null || quest.goals == null) return;

        bool changed = false;

        for (int i = 0; i < quest.goals.Count; i++)
        {
            var g = quest.goals[i];
            if (g == null) continue;
            if (g.type != QuestGoalType.Collect) continue;
            if (g.objectId != objectId) continue;

            for (int j = 0; j < goalStates.Count; j++)
            {
                if (goalStates[j].goalIndex != i) continue;

                var state = goalStates[j];
                state.currentCount = Mathf.Clamp(state.currentCount + amount, 0, state.targetCount);
                goalStates[j] = state;
                changed = true;
                break;
            }
        }

        if (changed)
            TryCompleteCurrentQuestServer();
    }

    public void AddInteractProgressServer(int objectId, int amount = 1)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        var quest = GetCurrentQuest();
        if (quest == null || quest.goals == null) return;

        bool changed = false;

        for (int i = 0; i < quest.goals.Count; i++)
        {
            var g = quest.goals[i];
            if (g == null) continue;
            if (g.type != QuestGoalType.Interact) continue;
            if (g.objectId != objectId) continue;

            for (int j = 0; j < goalStates.Count; j++)
            {
                if (goalStates[j].goalIndex != i) continue;

                var state = goalStates[j];
                state.currentCount = Mathf.Clamp(state.currentCount + amount, 0, state.targetCount);
                goalStates[j] = state;
                changed = true;
                break;
            }
        }

        if (changed)
            TryCompleteCurrentQuestServer();
    }

    public void SetCraftProgressServer(int objectId, int currentCount)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        var quest = GetCurrentQuest();
        if (quest == null || quest.goals == null) return;

        bool changed = false;

        for (int i = 0; i < quest.goals.Count; i++)
        {
            var g = quest.goals[i];
            if (g == null) continue;
            if (g.type != QuestGoalType.Craft) continue;
            if (g.objectId != objectId) continue;

            for (int j = 0; j < goalStates.Count; j++)
            {
                if (goalStates[j].goalIndex != i) continue;

                var state = goalStates[j];
                state.currentCount = Mathf.Clamp(currentCount, 0, state.targetCount);
                goalStates[j] = state;
                changed = true;
                break;
            }
        }

        if (changed)
            TryCompleteCurrentQuestServer();
    }

    private void TryCompleteCurrentQuestServer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        for (int i = 0; i < goalStates.Count; i++)
        {
            if (goalStates[i].currentCount < goalStates[i].targetCount)
                return;
        }

        int nextId = FindNextByFormer(currentQuestId.Value);
        if (nextId == 0)
            return;

        SetCurrentQuestServer(nextId);
    }

    private int FindNextByFormer(int formerId)
    {
        foreach (var q in questDB.GetAll())
        {
            if (q.Value.formerQuestId == formerId)
                return q.Value.questId;
        }
        return 0;
    }
}