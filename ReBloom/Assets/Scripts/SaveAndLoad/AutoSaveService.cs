using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class AutoSaveService : MonoBehaviour
{
    public static AutoSaveService I { get; private set; }

    [Header("Settings")]
    [SerializeField] private float debounceSeconds = 2f;
    [SerializeField] private float intervalSeconds = 60f;

    private bool dirty;
    private float lastRequestTime;
    private float lastSaveTime;
    private bool saving;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        lastSaveTime = Time.unscaledTime;
    }

    public void RequestSave(string reason = null)
    {
        dirty = true;
        lastRequestTime = Time.unscaledTime;
        Debug.Log($"[AutoSave] Request: {reason}");
    }

    public void MarkClean()
    {
        dirty = false;
        lastSaveTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!dirty || saving || SaveManager.I == null)
            return;

        if (SaveManager.I.IsLoading)
            return;

        float now = Time.unscaledTime;
        bool debounceOk = (now - lastRequestTime) >= debounceSeconds;
        bool intervalOk = (now - lastSaveTime) >= intervalSeconds;

        if (debounceOk || intervalOk)
            SaveInternal().Forget();
    }

    private async UniTask SaveInternal()
    {
        if (saving) return;
        saving = true;

        try
        {
            bool ok = await SaveManager.I.SaveAsync();
            if (ok)
            {
                dirty = false;
                lastSaveTime = Time.unscaledTime;
                Debug.Log($"[AutoSave] Saved slot={SaveManager.I.ActiveSlotId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoSave] Save failed: {e.Message}");
        }
        finally
        {
            saving = false;
        }
    }

    public async UniTask<bool> FlushAsync()
    {
        if (!dirty) return true;
        if (saving) await UniTask.WaitUntil(() => !saving);

        if (dirty)
            await SaveInternal();

        return !dirty;
    }
}
