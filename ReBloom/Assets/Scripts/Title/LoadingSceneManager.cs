using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider loadingBar;
    [SerializeField] private MultiplayerSaveCoordinator saveCoordinator;

    private void Awake()
    {
        saveCoordinator = MultiplayerSaveCoordinator.EnsureInstance();
    }

    private void Start()
    {
        LoadFlow().Forget();
    }

    private async UniTaskVoid LoadFlow()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
            return;

        float wait = 0f;
        const float minWait = 1.0f;
        const float maxWaitForClients = 8f;

        while (wait < maxWaitForClients)
        {
            wait += Time.deltaTime;

            int connected = nm.ConnectedClientsList.Count;
            if (connected >= 1 && wait >= minWait)
                break;

            if (loadingBar != null)
                loadingBar.value = Mathf.Clamp01(wait / Mathf.Max(minWait, maxWaitForClients));
            await UniTask.Yield();
        }

        await LoadMainNetcodeAsync("MainScene");
    }

    public async UniTask LoadMainNetcodeAsync(string sceneName = "MainScene")
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
            return;

        MultiplayerSaveCoordinator.BeginPendingLoadFlow();

        const float minShowTime = 2f;
        float elapsed = 0f;
        bool done = false;

        void OnLoadEventCompleted(string loadedSceneName, LoadSceneMode mode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (loadedSceneName != sceneName) return;
            done = true;
            nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }

        nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        if (nm.IsServer)
        {
            var status = nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[LoadingSceneManager] Netcode LoadScene failed: {status}");
                nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
                return;
            }
        }

        while (!done || elapsed < minShowTime)
        {
            elapsed += Time.deltaTime;
            float time01 = Mathf.Clamp01(elapsed / minShowTime);

            if (loadingBar != null)
                loadingBar.value = time01;

            await UniTask.Yield();
        }

        if (saveCoordinator != null)
            await saveCoordinator.HandlePostSceneLoadAsync();

        if (loadingBar != null)
            loadingBar.value = 1f;

        Debug.Log($"[LoadingSceneManager] Netcode scene load done: {sceneName}");
    }
}