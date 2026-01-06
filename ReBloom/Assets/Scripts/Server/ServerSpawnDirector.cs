using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnService : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private int spawnIndex;
    private NetworkManager nm;

    private void Awake()
    {
        nm = NetworkManager.Singleton;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (nm == null) nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        if (nm == null) return;
        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!nm.IsServer) return;
        if (sceneName != "MainScene") return;

        // MainScene 로드 끝난 시점에, 로드 완료된 클라들 전부 스폰 보장
        foreach (var clientId in clientsCompleted)
            EnsurePlayerSpawned(clientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!nm.IsServer) return;

        // 이미 MainScene인 상태에서 늦게 접속한 클라 처리
        if (SceneManager.GetActiveScene().name == "MainScene")
            EnsurePlayerSpawned(clientId);
    }

    private void EnsurePlayerSpawned(ulong clientId)
    {
        if (!nm.ConnectedClients.TryGetValue(clientId, out var client)) return;

        // 이미 있으면 스킵
        if (client.PlayerObject != null) return;

        var sp = spawnPoints[spawnIndex % spawnPoints.Length];
        spawnIndex++;

        var player = Instantiate(playerPrefab, sp.position, sp.rotation);
        player.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[Spawn] clientId={clientId} spawned at {sp.position}");
    }
}
