using System.Collections.Generic;
using UnityEngine;

public class DestroyedObjectRegistry : MonoBehaviour, ISaveable
{
    public static DestroyedObjectRegistry I { get; private set; }

    public string EntityGuid => "destroy_registry";

    private readonly HashSet<string> destroyed = new HashSet<string>();

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsDestroyed(string key) => !string.IsNullOrEmpty(key) && destroyed.Contains(key);

    public void MarkDestroyed(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (destroyed.Add(key))
            AutoSaveService.I?.RequestSave($"Destroyed:{key}");
    }

    public void Capture(SaveGameDTO save)
    {
        if (save.world == null) save.world = new WorldSaveDTO();
        save.world.destroyedKeys = new List<string>(destroyed);
    }

    public void Restore(SaveGameDTO save)
    {
        destroyed.Clear();
        if (save?.world?.destroyedKeys == null) return;

        foreach (var k in save.world.destroyedKeys)
            destroyed.Add(k);
    }
}
