using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider loadingBar;

    private async void Start()
    {
        await UniTask.Delay(300);

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

        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            loadingBar.value = elapsed / duration;

            Debug.Log($"Loading: {loadingBar.value * 100:F1}%");

            await UniTask.Yield();
        }

        loadingBar.value = 1f;
        await UniTask.Delay(300);
        asyncLoad.allowSceneActivation = true;
    }
}
