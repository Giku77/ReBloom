using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using System;
using System.Threading;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainTitleWindow : Window
{
    [SerializeField] private TextMeshProUGUI toastMessage;

    private CancellationTokenSource cts = new CancellationTokenSource();

    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    private void Start()
    {
        toastMessage.gameObject.SetActive(false);

        SoundManager.I.PlayTitleBGM();
    }

    public void OnGameStartButtonClicked()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnLoadGameButtonClicekd()
    { 
        OnNotImplementedButtonClickeddAsync().Forget();
    }

    public void OnSettingButtonClicked()
    {
        OnNotImplementedButtonClickeddAsync().Forget();
    }

    private async UniTask OnNotImplementedButtonClickeddAsync()
    {
        toastMessage.gameObject.SetActive(true);

        toastMessage.text = "추후 구현 예정입니다.";

        try
        {
            await UniTask.Delay(2000, cancellationToken: cts.Token);
            toastMessage.gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[MainTitleWindow] 메세지 안전하게 취소");
        }
    }

    public void OnQuitButtonClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
