using UnityEngine;

public class SettingWindow : Window
{
    [SerializeField] private SettingView view;
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private Window mainMenuWindow;

    private void OnEnable()
    {
        view.OnBackRequested += HandleBack;
        view.Show();
    }

    private void OnDisable()
    {
        view.OnBackRequested -= HandleBack;
        view.Hide();
    }

    private void HandleBack()
    {
        windowManager.ChangeWindow(mainMenuWindow);
    }
}
