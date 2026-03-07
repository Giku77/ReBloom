using Unity.Netcode;
using UnityEngine;

public class NetworkBuildManager : NetworkBehaviour
{
    public static NetworkBuildManager I { get; private set; }

    [SerializeField] private BuildManager buildManager;

    private void Awake()
    {
        I = this;
    }

    public override void OnDestroy()
    {
        if (I == this) I = null;
        base.OnDestroy();
    }

    // =========================
    // 클라 -> 서버 건축 요청
    // =========================
    public void RequestBuild(int arcId, Vector3 pos, Quaternion rot)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
            BuildServer(arcId, pos, rot, NetworkManager.Singleton.LocalClientId);
        else
            RequestBuildRpc(arcId, pos, rot);
    }

    [Rpc(SendTo.Server)]
    private void RequestBuildRpc(int arcId, Vector3 pos, Quaternion rot, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        BuildServer(arcId, pos, rot, senderId);
    }

    // =========================
    // 서버 실제 건축
    // =========================
    private void BuildServer(int arcId, Vector3 pos, Quaternion rot, ulong senderClientId)
    {
        if (!IsServer) return;
        if (buildManager == null)
        {
            Debug.LogError("[NetworkBuildManager] buildManager reference missing");
            return;
        }

        // (1) 플레이어 트랜스폼 가져오기 (서버 검증용)
        Transform builder = GetPlayerTransform(senderClientId);
        if (builder == null)
        {
            Debug.LogWarning($"[NetworkBuildManager] builder transform not found. clientId={senderClientId}");
            return;
        }

        // (2) ArcData
        if (!buildManager.ArcDB.TryGet(arcId, out var arc))
        {
            Debug.LogWarning($"[NetworkBuildManager] ARC_NOT_FOUND arcId={arcId}");
            return;
        }

        // (3) 서버에서 구역 제한도 재확인 (치트 방지)
        if (!buildManager.IsInBuildableZone_Server(builder))
        {
            Debug.LogWarning($"[NetworkBuildManager] Not buildable zone. clientId={senderClientId}");
            return;
        }

        // (4) 서버에서 다시 배치 검증 + 바닥 보정
        if (!buildManager.CanBuildAt_Server(arc, pos, rot, builder, out string errorCode, out Vector3 adjustedPos))
        {
            Debug.LogWarning($"[NetworkBuildManager] Build denied: {errorCode}");
            return;
        }

        // (5) 재료 검사/차감(서버 권위)
        var inv = ResolvePlayerInventory(senderClientId);
        if (inv == null)
        {
            Debug.LogWarning($"[NetworkBuildManager] inventory not found for clientId={senderClientId}");
            return;
        }

        if (buildManager.TryGetRecipe(arcId, out var recipe))
        {
            if (!buildManager.HasMaterials(inv, recipe))
            {
                Debug.LogWarning("[NetworkBuildManager] Not enough materials");
                return;
            }

            buildManager.Remove(inv, recipe);
        }

        // (6) 서버에서 네트워크 스폰
        var prefab = arc.buildPrefab != null ? arc.buildPrefab : buildManager.prefab;
        if (prefab == null)
        {
            Debug.LogError($"[NetworkBuildManager] prefab missing arcId={arcId}");
            return;
        }

        var go = Instantiate(prefab, adjustedPos, rot);

        // arcId 세팅
        var bInst = go.GetComponent<BuildingInstance>();
        if (bInst != null) bInst.arcId = arc.arcId;

        // SaveableEntity GUID는 "서버에서만" 만드는 게 정석 (중복 방지)
        var id = go.GetComponent<SaveableEntity>();
        if (id != null) id.AssignNewId();

        var ws = go.GetComponent<WorldStorage>();
        if (ws != null && id != null)
            ws.SetContainerGuid($"container:{id.PersistentId}");

        var netObj = go.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[NetworkBuildManager] NetworkObject missing on prefab {prefab.name}");
            Destroy(go);
            return;
        }

        netObj.Spawn(true);

        // 서버 기준 퀘스트/저장
        NetworkQuestManager.I?.ReportCraft(arc.arcId);
        AutoSaveService.I?.RequestSave("Build");
    }

    // =========================
    // 삭제 요청/서버 삭제
    // =========================
    public void RequestRemove(ulong buildingNetworkObjectId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
            RemoveServer(buildingNetworkObjectId, NetworkManager.Singleton.LocalClientId);
        else
            RequestRemoveRpc(buildingNetworkObjectId);
    }

    [Rpc(SendTo.Server)]
    private void RequestRemoveRpc(ulong buildingNetworkObjectId, RpcParams rpcParams = default)
    {
        RemoveServer(buildingNetworkObjectId, rpcParams.Receive.SenderClientId);
    }

    private void RemoveServer(ulong buildingNetworkObjectId, ulong senderClientId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(buildingNetworkObjectId, out var netObj))
            return;

        var inst = netObj.GetComponent<BuildingInstance>();
        if (inst == null) return;

        // TODO: 권한 체크 (누가 지은 건물인지, 제거 가능 여부 등)
        netObj.Despawn(true);

        AutoSaveService.I?.RequestSave("RemoveBuilding");
    }

    // =========================
    // 헬퍼
    // =========================
    private Transform GetPlayerTransform(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return null;

        if (nm.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            return client.PlayerObject.transform;

        return null;
    }

    private PlayerInventoryRuntime ResolvePlayerInventory(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return null;

        if (nm.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            return client.PlayerObject.GetComponent<PlayerInventoryRuntime>();

        return null;
    }
}