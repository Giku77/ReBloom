using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainTitleWindow : Window
{
    public void OnGameStartButtonClicked()
    {
        SceneManager.LoadSceneAsync("MainScene");
    }
}
