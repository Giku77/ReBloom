using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider loadingBar;

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
