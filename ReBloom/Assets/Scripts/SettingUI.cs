using UnityEngine;

public class SettingUI : UIBase
{
    [SerializeField] private SettingView view;

    protected override void OnShow()
    {
        Time.timeScale = 0f;

        UIManager.Instance.SetPaused(true);

        SoundManager.I?.PlayOpenInventory();

        view.OnBackRequested += HandleBack;
        view.Show();
    }

    protected override void OnHide()
    {
        view.OnBackRequested -= HandleBack;
        view.Hide();
        UIManager.Instance.ShowUI(UIType.GamePause);

        //Time.timeScale = 1f;
        UIManager.Instance.SetPaused(false);
        SoundManager.I?.PlayCloseInventory();
        AutoSaveService.I?.RequestSave("Settings");
    }

    private void HandleBack()
    {
        OnHide();
    }
}
