using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;

    private void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SceneManager == null)
            return;

        nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SceneManager == null)
            return;

        nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode mode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
            return;

        if (sceneName != "MainScene")
            return;

        if (FindFirstObjectByType<PlayerSpawnService>() != null)
        {
            Debug.Log("[PlayerSpawner] PlayerSpawnService가 존재하므로 fallback 스폰을 건너뜁니다.");
            return;
        }

        foreach (var clientId in nm.ConnectedClientsIds)
        {
            if (!nm.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            if (client.PlayerObject != null)
                continue;

            var player = Instantiate(playerPrefab);
            player.SpawnAsPlayerObject(clientId, true);
            Debug.LogWarning($"[PlayerSpawner] PlayerSpawnService가 없어 fallback 위치에서 플레이어를 생성했습니다. clientId={clientId}");
        }
    }
}
