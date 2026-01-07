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
        if (!nm.IsServer) return;

        nm.OnClientConnectedCallback += OnClientConnected;
        StartCoroutine(SpawnAllNextFrame());
    }

    private IEnumerator SpawnAllNextFrame()
    {
        yield return null;
        var nm = NetworkManager.Singleton;

        Debug.Log($"[Spawn] ConnectedClientsIds = {string.Join(",", nm.ConnectedClientsIds)}");
        foreach (var id in nm.ConnectedClientsIds)
            EnsureSpawn(id);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(SpawnLate(clientId));
    }

    private IEnumerator SpawnLate(ulong clientId)
    {
        yield return null;
        EnsureSpawn(clientId);
    }

    private void EnsureSpawn(ulong clientId)
    {
        var nm = NetworkManager.Singleton;

        Debug.Log($"[Spawn] Ensure clientId={clientId} spawnPoints={(spawnPoints==null?-1:spawnPoints.Length)}");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Spawn] spawnPoints 비었음. PlayerSpawnService를 MainScene에 두고 spawnPoints를 MainScene 오브젝트로 연결해야 함.");
            return;
        }

        if (!nm.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Debug.LogWarning($"[Spawn] ConnectedClients에 {clientId} 없음");
            return;
        }

        if (client.PlayerObject != null)
        {
            Debug.Log($"[Spawn] clientId={clientId} already has PlayerObject");
            return;
        }

        var sp = spawnPoints[spawnIndex % spawnPoints.Length];
        spawnIndex++;

        var p = Instantiate(playerPrefab, sp.position, sp.rotation);
        p.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[Spawn] spawned clientId={clientId} owner={p.OwnerClientId} netId={p.NetworkObjectId} pos={sp.position}");
    }
}
