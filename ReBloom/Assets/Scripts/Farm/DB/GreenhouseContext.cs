using UnityEngine;
using System;
using System.Collections.Generic;

public class GreenhouseContext : MonoBehaviour
{
    [Serializable]
    public class ActivatableEntry
    {
        public string key;       // "Pot2", "Sprinkler", "Drone"...
        public GameObject target; // 실제 GO
    }

    [Header("Identity")]
    [SerializeField] private string greenhouseInstanceId;

    [Header("Activatables (Key -> GameObject)")]
    [SerializeField] private List<ActivatableEntry> activatables = new();

    private readonly Dictionary<string, List<GameObject>> _map = new();

    public string Id => greenhouseInstanceId;

    private void Awake()
    {
        EnsureId();
        RebuildMap();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureId();
            RebuildMap();
        }
    }
#endif

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(greenhouseInstanceId))
            greenhouseInstanceId = Guid.NewGuid().ToString("N");
    }

    private void RebuildMap()
    {
        _map.Clear();

        foreach (var a in activatables)
        {
            if (a == null || string.IsNullOrWhiteSpace(a.key) || a.target == null) 
                continue;

            if (!_map.TryGetValue(a.key, out var list))
            {
                list = new List<GameObject>();
                _map.Add(a.key, list);
            }
            list.Add(a.target);
        }
    }

    public bool TryActivate(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "0") return false;

        if (_map.TryGetValue(key, out var list) && list != null && list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i].SetActive(true);
            }
            return true;
        }

        Debug.LogWarning($"[GreenhouseContext] Activatable key not found: {key} (greenhouseId={greenhouseInstanceId})");
        return false;
    }
}
