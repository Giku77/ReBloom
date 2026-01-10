using UnityEngine;

public static class JoinCodeStore
{
    private const string Key = "relay_join_code";
    public static string Current
    {
        get => PlayerPrefs.GetString(Key, "");
        set => PlayerPrefs.SetString(Key, value);
    }
}
