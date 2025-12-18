using UnityEngine;

public class SaveManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (SaveManager.I != null) return;

        var prefab = Resources.Load<SaveManager>("SaveManager");
        if (prefab != null)
        {
            Object.Instantiate(prefab);
            return;
        }
    }
}
