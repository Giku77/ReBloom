using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider loadingBar;

    private async void Start()
    {
        Time.timeScale = 1f;

        await UniTask.Delay(100);

        await LoadSceneAsync("MainScene");
    }

    //private async UniTask LoadSceneAsync(string sceneName)
    //{
    //    var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
    //    asyncLoad.allowSceneActivation = false;

    //    while (asyncLoad.progress < 0.9f)
    //    {
    //        float progress = asyncLoad.progress / 0.9f;
    //        loadingBar.value = progress;

    //        Debug.Log($"Loading: {progress * 100:F1}%");
    //        await UniTask.Yield();
    //    }

    //    await UniTask.Delay(500);

    //    asyncLoad.allowSceneActivation = true;
    //}

    private async UniTask LoadSceneAsync(string sceneName)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        const float minShowTime = 2f;
        float elapsed = 0f;

        while (asyncLoad.progress < 0.9f || elapsed < minShowTime)
        {
            elapsed += Time.deltaTime;

            float real = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            float time = Mathf.Clamp01(elapsed / minShowTime);

            loadingBar.value = Mathf.Max(real, time);

            await UniTask.Yield();
        }

        loadingBar.value = 1f;
        asyncLoad.allowSceneActivation = true;
    }
}
