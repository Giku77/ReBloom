using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnService : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private int spawnIndex;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
            return;

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        StartCoroutine(SpawnAllNextFrame());
    }

    private void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
            return;

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private IEnumerator SpawnAllNextFrame()
    {
        var nm = NetworkManager.Singleton;

        float timeout = 2f;
        float t = 0f;

        while (t < timeout && nm.ConnectedClientsIds.Count < 2)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log($"[Spawn] ConnectedClientsIds = {string.Join(",", nm.ConnectedClientsIds)} (waited {t:F2}s)");

        foreach (var id in nm.ConnectedClientsIds)
            EnsureSpawn(id);
    }

    private void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
            return;

        StartCoroutine(SpawnLate(clientId));
    }

    private void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer || nm.ShutdownInProgress || clientId == NetworkManager.ServerClientId)
            return;

        StartCoroutine(CleanupDisconnectedPlayerNextFrame(clientId));
    }

    private IEnumerator SpawnLate(ulong clientId)
    {
        yield return null;
        EnsureSpawn(clientId);
    }

    private IEnumerator CleanupDisconnectedPlayerNextFrame(ulong clientId)
    {
        yield return null;
        CleanupDisconnectedPlayer(clientId);
    }

    private void EnsureSpawn(ulong clientId)
    {
        var nm = NetworkManager.Singleton;

        Debug.Log($"[Spawn] Ensure clientId={clientId} spawnPoints={(spawnPoints == null ? -1 : spawnPoints.Length)}");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Spawn] spawnPoints is empty. Place PlayerSpawnService in MainScene and assign scene spawn points.");
            return;
        }

        if (!nm.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Debug.LogWarning($"[Spawn] ConnectedClients missing clientId={clientId}");
            return;
        }

        if (client.PlayerObject != null)
        {
            Debug.Log($"[Spawn] clientId={clientId} already has PlayerObject");
            return;
        }

        var sp = spawnPoints[spawnIndex % spawnPoints.Length];
        spawnIndex++;

        var playerObject = Instantiate(playerPrefab, sp.position, sp.rotation);
        playerObject.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[Spawn] spawned clientId={clientId} owner={playerObject.OwnerClientId} netId={playerObject.NetworkObjectId} pos={sp.position}");
    }

    private void CleanupDisconnectedPlayer(ulong clientId)
    {
        bool removedAny = false;
        var gates = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);

        foreach (var gate in gates)
        {
            if (gate == null || gate.OwnerClientId != clientId)
                continue;

            var networkObject = gate.GetComponent<NetworkObject>();
            if (networkObject == null)
                continue;

            removedAny = true;

            if (networkObject.IsSpawned)
            {
                Debug.Log($"[Spawn] Despawning disconnected player clientId={clientId} netId={networkObject.NetworkObjectId}");
                networkObject.Despawn(true);
            }
            else
            {
                Debug.Log($"[Spawn] Destroying stale disconnected player clientId={clientId}");
                Destroy(networkObject.gameObject);
            }
        }

        if (!removedAny)
            Debug.Log($"[Spawn] No lingering player object found for disconnected clientId={clientId}");
    }
}
