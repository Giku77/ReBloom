using System.Collections.Generic;
using UnityEngine;

public static class SaveRegistry
{
    public static List<ISaveable> FindAllSaveablesInScene()
    {
        var list = new List<ISaveable>();

        var monos = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var m in monos)
        {
            if (m is ISaveable s)
                list.Add(s);
        }
        return list;
    }
}
