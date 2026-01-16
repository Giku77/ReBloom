using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/Build Message Table")]
public class BuildMessageTable : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public BuildError error;
        [TextArea] public string message;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<BuildError, string> _cache;

    private void OnEnable()
    {
        _cache = new Dictionary<BuildError, string>();
        foreach (var e in entries)
            _cache[e.error] = e.message;
    }

    public string Resolve(BuildError error)
    {
        if (error == BuildError.None) return "";
        if (_cache != null && _cache.TryGetValue(error, out var msg) && !string.IsNullOrEmpty(msg))
            return msg;

        return $"알 수 없는 오류: {error}";
    }
}
