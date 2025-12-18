using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class AutoSaveService : MonoBehaviour
{
    public static AutoSaveService I { get; private set; }

    [Header("Settings")]
    [SerializeField] private float debounceSeconds = 2f;   // 요청 후 2초 지나면 저장
    [SerializeField] private float intervalSeconds = 60f;  // 60초마다 dirty면 저장
    [SerializeField] private string slotId = "slot1";

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

        // 디버그용
        Debug.Log($"[AutoSave] Request: {reason}");
    }

    public void MarkClean()
    {
        dirty = false;
        lastSaveTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!dirty) return;
        if (saving) return;
        if (SaveManager.I == null) return;

        if (SaveManager.I.IsLoading) return;


        float now = Time.unscaledTime;

        bool debounceOk = (now - lastRequestTime) >= debounceSeconds;
        
        bool intervalOk = (now - lastSaveTime) >= intervalSeconds;

        if (debounceOk || intervalOk)
        {   
            SaveInternal().Forget();
        }
    }

    private async UniTask SaveInternal()
    {
        if (saving) return;
        saving = true;

        try
        {
            bool ok = await SaveManager.I.SaveAsync(slotId);
            if (ok)
            {
                dirty = false;
                lastSaveTime = Time.unscaledTime;
                Debug.Log("[AutoSave] Saved");
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

    /// 메뉴 버튼에서 "저장 완료를 보장"하고 싶을 때 사용
    public async UniTask<bool> FlushAsync()
    {
        if (!dirty) return true;
        if (saving) await UniTask.WaitUntil(() => !saving);

        // dirty가 여전히 true면 직접 한 번 저장
        if (dirty)
        {
            await SaveInternal();
        }
        return !dirty; // 저장 성공하면 dirty=false
    }
}
