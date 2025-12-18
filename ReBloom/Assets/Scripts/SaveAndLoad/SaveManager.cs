using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I { get; private set; }

    [SerializeField] private bool compressGzip = true;
    [SerializeField] private bool usePlayFab = true;

    public bool IsLoading { get; private set; }
    public bool HasLoadedOnce { get; private set; }

    private ISaveStorage storage;
    private bool ready;

    private async void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (usePlayFab)
        {
            await PlayFabAuth.LoginAsync();
            storage = new PlayFabSaveStorage();
        }
        else
        {
            storage = new LocalFileSaveStorage();
        }

        ready = true;
    }

    public async UniTask<bool> HasSaveAsync(string slotId = "slot1")
    {
        await WaitReadyAsync();
        return await storage.ExistsAsync(slotId);
    }

    public async UniTask<bool> SaveAsync(string slotId = "slot1")
    {
        await WaitReadyAsync();

        if (IsLoading)
        {
            Debug.LogWarning($"[SaveAsync] blocked IsLoading={IsLoading} HasLoadedOnce={HasLoadedOnce}");
            return false;
        }

        Debug.Log("[SaveAsync] Start");

        var save = new SaveGameDTO
        {
            meta =
            {
                slotId = slotId,
                version = SaveConstants.SAVE_VERSION,
                sceneName = SceneManager.GetActiveScene().name,
                savedAtUtcTicks = DateTime.UtcNow.Ticks,
                commitId = Guid.NewGuid().ToString("N")
            }
        };

        var saveables = SaveRegistry.FindAllSaveablesInScene();
        Debug.Log("[Save] saveables = " + string.Join(", ", saveables.ConvertAll(s => s.GetType().Name)));
        foreach (var s in saveables)
            s.Capture(save);

        var bytes = SaveSerializerNewtonsoft.ToBytes(save, compressGzip);
        await storage.SaveAsync(slotId, bytes);

        Debug.Log($"[SaveAsync] slot={slotId} containers={save.world.containers.Count}");
        Debug.Log($"[SaveAsync] buildings={save.world.placedBuildings.Count} containers={save.world.containers.Count}");
        return true;
    }

    private async UniTask WaitReadyAsync()
    {
        await UniTask.WaitUntil(() => ready && storage != null);
    }

    public async UniTask<bool> LoadAsync(string slotId = "slot1")
    {
        await WaitReadyAsync();

        IsLoading = true;
        try
        {

            var bytes = await storage.LoadAsync(slotId);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning($"[LoadAsync] No data slot={slotId}");
                return false;
            }

            var save = SaveSerializerNewtonsoft.FromBytes<SaveGameDTO>(bytes, compressGzip);

            // (선택) 저장된 씬과 다르면 씬 로드 후 Restore 해야 함
            if (!string.IsNullOrEmpty(save.meta.sceneName) &&
                SceneManager.GetActiveScene().name != save.meta.sceneName)
            {
                await SceneManager.LoadSceneAsync(save.meta.sceneName);
            }

            await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null);
            await UniTask.DelayFrame(1);

            var saveables = SaveRegistry.FindAllSaveablesInScene();
            foreach (var s in saveables)
                s.Restore(save);

            HasLoadedOnce = true;

            AutoSaveService.I?.MarkClean();

            Debug.Log($"[LoadAsync] slot={slotId} commit={save.meta.commitId}");
            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        #if UNITY_EDITOR
            return;
        #else
            if (pause) AutoSaveService.I?.FlushAsync().Forget(); // 또는 SaveAsync
        #endif
    }

    private void OnApplicationQuit()
    {
        #if UNITY_EDITOR
            return;
        #else
            SaveAsync("slot1").Forget();
        #endif
    }

    public async UniTask<bool> ResetSlotAsync(string slotId = "slot1", bool saveDefaultImmediately = true)
    {
        await WaitReadyAsync();

        await storage.DeleteAsync(slotId);

        if (!saveDefaultImmediately)
            return true;

        // 기본 상태를 저장하고 싶으면:
        // 1) 새 게임 상태로 씬/플레이어/인벤 초기화
        // 2) SaveAsync 호출

        // 예: 현재 씬에서 전부 초기화하고 저장
        // (너희 프로젝트 초기화 로직에 맞춰 구현)
        await SaveAsync(slotId);
        return true;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.homeKey.wasPressedThisFrame)
        {
            ResetSlotAsync("slot1", saveDefaultImmediately:false).Forget();
        }
    }

}
