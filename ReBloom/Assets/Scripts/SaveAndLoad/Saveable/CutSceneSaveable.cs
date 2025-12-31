using UnityEngine;

public class CutSceneSaveable : MonoBehaviour, ISaveable
{
    public string EntityGuid => "cutscene";

    [SerializeField] private CutSceneManager cutSceneManager;

    private void Awake()
    {
        if (cutSceneManager == null)
            cutSceneManager = FindFirstObjectByType<CutSceneManager>();
    }

    public void Capture(SaveGameDTO save)
    {
        if (cutSceneManager == null) return;
        save.cutScene.introCutsceneSeen = cutSceneManager.IntroCutsceneSeen;
    }

    public void Restore(SaveGameDTO save)
    {
        if (cutSceneManager == null) return;
        cutSceneManager.SetIntroCutsceneSeen(save.cutScene?.introCutsceneSeen ?? false);
    }
}
