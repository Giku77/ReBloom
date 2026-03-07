using Cysharp.Threading.Tasks;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum QuestFlowState : byte
{
    Active = 0,
    CompletedAwaitingHost = 1
}

public class NetworkQuestManager : NetworkBehaviour
{
    public static NetworkQuestManager I { get; private set; }

    [SerializeField] private QuestDB questDB;

    private NetworkVariable<int> currentQuestId = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<QuestGoalProgressState> goalStates;

    private NetworkVariable<QuestFlowState> flowState = new(
    QuestFlowState.Active,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    private NetworkVariable<int> pendingNextQuestId = new(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsAwaitingHostAdvance =>
    flowState.Value == QuestFlowState.CompletedAwaitingHost;

    private int _firstQuestId;
    private NetworkVariable<bool> firstQuestCompleted = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    private void OnFlowStateChanged(QuestFlowState oldV, QuestFlowState newV)
    {
        OnQuestUpdated?.Invoke();
    }
    private void OnPendingNextChanged(int oldV, int newV)
    {
        OnQuestUpdated?.Invoke();
    }

    private void OnFirstQuestCompletedChanged(bool oldV, bool newV)
    {
        if (newV)
            FirstQuestCompletedClientRpc();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[NQM] OnNetworkSpawn IsServer={IsServer} IsClient={IsClient} IsSpawned={IsSpawned} questDB={(questDB != null)} currentQuestId={currentQuestId.Value}");
        currentQuestId.OnValueChanged += OnQuestIdChanged;
        flowState.OnValueChanged += OnFlowStateChanged;
        pendingNextQuestId.OnValueChanged += OnPendingNextChanged;
        goalStates.OnListChanged += OnGoalStatesChanged;
        firstQuestCompleted.OnValueChanged += OnFirstQuestCompletedChanged;

        if (IsServer && currentQuestId.Value == 0)
            InitServerDeferred().Forget();

        if (!IsServer && firstQuestCompleted.Value)
            FirstQuestCompletedClientRpc();

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
        flowState.OnValueChanged -= OnFlowStateChanged;
        pendingNextQuestId.OnValueChanged -= OnPendingNextChanged;
        goalStates.OnListChanged -= OnGoalStatesChanged;
        firstQuestCompleted.OnValueChanged -= OnFirstQuestCompletedChanged;
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
                _firstQuestId = q.questId;
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
        flowState.Value = QuestFlowState.Active;
        pendingNextQuestId.Value = 0;
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

        RefreshCollectProgressServer();
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
        RefreshCollectProgressServer(objectId);
    }

    public void RefreshCollectProgressServer()
    {
        RefreshCollectProgressServer(-1);
    }

    public void RefreshCollectProgressServer(int objectId)
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
            if (objectId > 0 && g.objectId != objectId) continue;

            int currentCount = GetSharedInventoryCount(g.objectId);

            for (int j = 0; j < goalStates.Count; j++)
            {
                if (goalStates[j].goalIndex != i) continue;

                var state = goalStates[j];
                int nextCount = Mathf.Clamp(currentCount, 0, state.targetCount);
                if (state.currentCount == nextCount)
                    break;

                state.currentCount = nextCount;
                goalStates[j] = state;
                changed = true;
                break;
            }
        }

        if (changed)
            TryCompleteCurrentQuestServer();
    }

    private int GetSharedInventoryCount(int itemId)
    {
        if (NetworkManager.Singleton == null)
            return 0;

        int total = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var inventory = playerObject.GetComponent<PlayerInventoryRuntime>();
            if (inventory == null)
                continue;

            total += inventory.GetItemCount(itemId);
        }

        return total;
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
        if (flowState.Value != QuestFlowState.Active) return;

        for (int i = 0; i < goalStates.Count; i++)
        {
            if (goalStates[i].currentCount < goalStates[i].targetCount)
                return;
        }

        int nextId = FindNextByFormer(currentQuestId.Value);
        pendingNextQuestId.Value = nextId; 

        flowState.Value = QuestFlowState.CompletedAwaitingHost;

        PlayQuestCompleteFxClientRpc();
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

    // 호스트/서버만 다음 퀘스트로 진행
    public void RequestAdvanceFromHost()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            AdvanceServerInternal();
            return;
        }

        RequestAdvanceFromHostRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestAdvanceFromHostRpc(RpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton == null) return;

        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            return;

        AdvanceServerInternal();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayQuestCompleteFxClientRpc()
    {
        PlayQuestCompleteFxDeferred().Forget();
    }

    private async UniTaskVoid PlayQuestCompleteFxDeferred()
    {
        await UniTask.WaitUntil(() => QuestManager.I != null);
        QuestManager.I.PlayQuestCompleteAnimationForced();
    }

    [Rpc(SendTo.Everyone)]
    private void HideQuestCompleteFxClientRpc()
    {
        HideQuestCompleteFxDeferred().Forget();
    }

    private async UniTaskVoid HideQuestCompleteFxDeferred()
    {
        await UniTask.WaitUntil(() => QuestManager.I != null);
        QuestManager.I.HideQuestCompleteAnimation();
    }

    private void AdvanceServerInternal()
    {
        if (!IsServer) return;
        if (flowState.Value != QuestFlowState.CompletedAwaitingHost) return;

        int prevQuestId = currentQuestId.Value;      
        int nextId = pendingNextQuestId.Value;
        if (nextId == 0) return;

        if (!firstQuestCompleted.Value && prevQuestId == _firstQuestId)
        {
            firstQuestCompleted.Value = true;
            FirstQuestCompletedClientRpc();         
        }

        HideQuestCompleteFxClientRpc();
        SetCurrentQuestServer(nextId);
    }

    [Rpc(SendTo.Everyone)]
    private void FirstQuestCompletedClientRpc()
    {
        FirstQuestCompletedDeferred().Forget();
    }

    private async UniTaskVoid FirstQuestCompletedDeferred()
    {
        await UniTask.WaitUntil(() => QuestManager.I != null);
        QuestManager.I.SetFirstQuest(true);
    }
}
