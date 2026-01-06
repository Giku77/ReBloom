using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider loadingBar;

    private void Start()
    {
        LoadFlow().Forget();
    }

    private async UniTaskVoid LoadFlow()
    {
        var nm = NetworkManager.Singleton;

        // 서버만 씬 로드 권한
        if (!nm.IsServer)
            return;

        // 1) 최소 표시 시간(UX)
        float wait = 0f;
        const float minWait = 1.0f;

        // 2) 클라가 붙을 시간을 줌 (원하면 0명이어도 진행 가능하게)
        const float maxWaitForClients = 8f;   // 8초 기다리고 그냥 진행
        while (wait < maxWaitForClients)
        {
            wait += Time.deltaTime;

            int connected = nm.ConnectedClientsList.Count; // 호스트 포함
            // 혼자 플레이도 OK면 1이면 통과, 2명 필요면 2로 바꾸면 됨
            if (connected >= 1 && wait >= minWait)
                break;

            loadingBar.value = Mathf.Clamp01(wait / Mathf.Max(minWait, maxWaitForClients));
            await UniTask.Yield();
        }

        // 이제 Netcode Scene Load
        await LoadMainNetcodeAsync("MainScene");
    }


    public async UniTask LoadMainNetcodeAsync(string sceneName = "MainScene")
    {
        var nm = NetworkManager.Singleton;

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

            // 실제 진행률 대신, 최소 표시 시간 기준으로 부드럽게
            loadingBar.value = time01;

            await UniTask.Yield();
        }

        loadingBar.value = 1f;
        Debug.Log($"[LoadingSceneManager] Netcode scene load done: {sceneName}");
    }
}
