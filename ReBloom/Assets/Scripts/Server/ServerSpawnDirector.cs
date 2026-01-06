using System.Collections;
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
        System.Collections.Generic.List<ulong> clientsCompleted,
        System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (!nm.IsServer) return;
        if (sceneName != "MainScene") return;

        // 핵심: clientsCompleted 말고 "현재 접속 중 전체"를 대상으로 스폰 보장
        StartCoroutine(SpawnAllConnectedNextFrame());
    }

    private IEnumerator SpawnAllConnectedNextFrame()
    {
        yield return null; // 1프레임 대기 (ConnectedClients 갱신 안정화)

        foreach (var clientId in nm.ConnectedClientsIds)
            EnsurePlayerSpawned(clientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!nm.IsServer) return;

        // 메인씬 이미 켜진 상태에서 늦게 들어온 애 즉시 스폰
        if (SceneManager.GetActiveScene().name == "MainScene")
            StartCoroutine(SpawnLateJoinerNextFrame(clientId));
    }

    private IEnumerator SpawnLateJoinerNextFrame(ulong clientId)
    {
        yield return null;
        EnsurePlayerSpawned(clientId);
    }

    private void EnsurePlayerSpawned(ulong clientId)
    {
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

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Spawn] spawnPoints 비었음 (MainScene 오브젝트 참조 문제 가능)");
            return;
        }

        var sp = spawnPoints[spawnIndex % spawnPoints.Length];
        spawnIndex++;

        var player = Instantiate(playerPrefab, sp.position, sp.rotation);
        player.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[Spawn] clientId={clientId} spawned. owner={player.OwnerClientId} netId={player.NetworkObjectId}");
    }
}
