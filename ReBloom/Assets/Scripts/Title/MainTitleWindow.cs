using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainTitleWindow : Window
{
    [SerializeField] private TextMeshProUGUI toastMessage;

    private void Start()
    {
        toastMessage.gameObject.SetActive(false);
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

        await UniTask.Delay(2000);

        toastMessage.gameObject.SetActive(false);
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
