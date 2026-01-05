using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I { get; private set; }

    [Header("Serialize")]
    [SerializeField] private bool compressGzip = true;

    [Header("Remote")]
    [SerializeField] private bool usePlayFab = true;

    public bool IsLoading { get; private set; }
    public bool HasLoadedOnce { get; private set; }
    public bool RemoteReady => remoteReady;

    private ISaveStorage localStorage;
    private ISaveStorage remoteStorage;

    private bool ready;
    private bool remoteReady;

    private const string PendingUploadKeyPrefix = "save_pending_upload_";

    private async void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        localStorage = new LocalFileSaveStorage();

        remoteReady = false;
        remoteStorage = null;

        if (usePlayFab)
        {
            try
            {
                await PlayFabAuth.LoginAsync();
                remoteStorage = new PlayFabSaveStorage();
                remoteReady = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Remote init failed. Local only. {e}");
                remoteReady = false;
                remoteStorage = null;
            }
        }

        ready = (localStorage != null);
    }

    private async UniTask WaitReadyAsync()
    {
        await UniTask.WaitUntil(() => ready && localStorage != null);
    }

    // ----------------------------
    // Public API
    // ----------------------------

    public async UniTask<bool> HasSaveAsync(string slotId = "slot1")
    {
        await WaitReadyAsync();

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                if (await remoteStorage.ExistsAsync(slotId)) return true;
            }
            catch { /* ignore */ }
        }

        return await localStorage.ExistsAsync(slotId);
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

        var save = BuildSaveDTO(slotId);

        var saveables = SaveRegistry.FindAllSaveablesInScene();
        Debug.Log("[Save] saveables = " + string.Join(", ", saveables.ConvertAll(s => s.GetType().Name)));
        foreach (var s in saveables)
            s.Capture(save);

        var bytes = SaveSerializerNewtonsoft.ToBytes(save, compressGzip);

        await localStorage.SaveAsync(slotId, bytes);

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                await remoteStorage.SaveAsync(slotId, bytes);
                SetPendingUpload(slotId, false);
                Debug.Log($"[SaveAsync] Remote upload OK slot={slotId}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveAsync] Remote upload failed -> pending. {e}");
                SetPendingUpload(slotId, true);
            }
        }
        else
        {
            SetPendingUpload(slotId, true);
        }

        Debug.Log($"[SaveAsync] slot={slotId} buildings={save.world.placedBuildings.Count} containers={save.world.containers.Count}");
        return true;
    }

    public async UniTask<bool> LoadAsync(string slotId = "slot1")
    {
        await WaitReadyAsync();

        IsLoading = true;
        try
        {
            byte[] remoteBytes = null;
            byte[] localBytes = null;

            if (remoteReady && remoteStorage != null)
            {
                try { remoteBytes = await remoteStorage.LoadAsync(slotId); }
                catch (Exception e) { Debug.LogWarning($"[LoadAsync] Remote load failed. {e}"); }
            }

            try { localBytes = await localStorage.LoadAsync(slotId); }
            catch (Exception e) { Debug.LogWarning($"[LoadAsync] Local load failed. {e}"); }

            if ((remoteBytes == null || remoteBytes.Length == 0) &&
                (localBytes == null || localBytes.Length == 0))
            {
                Debug.LogWarning($"[LoadAsync] No data slot={slotId} (remote & local empty)");
                return false;
            }

            SaveGameDTO remoteSave = null;
            SaveGameDTO localSave = null;

            if (remoteBytes != null && remoteBytes.Length > 0)
            {
                try { remoteSave = SaveSerializerNewtonsoft.FromBytes<SaveGameDTO>(remoteBytes, compressGzip); }
                catch (Exception e) { Debug.LogWarning($"[LoadAsync] Remote deserialize failed. {e}"); }
            }

            if (localBytes != null && localBytes.Length > 0)
            {
                try { localSave = SaveSerializerNewtonsoft.FromBytes<SaveGameDTO>(localBytes, compressGzip); }
                catch (Exception e) { Debug.LogWarning($"[LoadAsync] Local deserialize failed. {e}"); }
            }

            var chosen = ChooseNewer(remoteSave, localSave);
            if (chosen == null)
            {
                Debug.LogWarning($"[LoadAsync] Both saves invalid slot={slotId}");
                return false;
            }

            if (chosen == localSave && remoteReady && remoteStorage != null)
            {
                SetPendingUpload(slotId, true);
            }

            if (SettingManager.I != null && chosen.settings != null)
                SettingManager.I.Apply(chosen.settings);

            if (!string.IsNullOrEmpty(chosen.meta.sceneName) &&
                SceneManager.GetActiveScene().name != chosen.meta.sceneName)
            {
                await SceneManager.LoadSceneAsync(chosen.meta.sceneName);
            }

            await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null);
            await UniTask.DelayFrame(1);

            var saveables = SaveRegistry.FindAllSaveablesInScene();
            foreach (var s in saveables)
                s.Restore(chosen);

            await UniTask.DelayFrame(1);

            var saveables2 = SaveRegistry.FindAllSaveablesInScene();
            foreach (var s in saveables2)
                if (s is not WorldBuildingsSaveable) s.Restore(chosen);

            HasLoadedOnce = true;
            AutoSaveService.I?.MarkClean();

            Debug.Log($"[LoadAsync] slot={slotId} commit={chosen.meta.commitId} savedAt={chosen.meta.savedAtUtcTicks} (remoteReady={remoteReady})");

            TryFlushPendingUpload(slotId).Forget();

            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async UniTask<bool> ResetSlotAsync(string slotId = "slot1", bool saveDefaultImmediately = true)
    {
        await WaitReadyAsync();

        try { await localStorage.DeleteAsync(slotId); } catch { }
        if (remoteReady && remoteStorage != null)
        {
            try { await remoteStorage.DeleteAsync(slotId); } catch { }
        }

        SetPendingUpload(slotId, false);

        if (!saveDefaultImmediately)
            return true;

        await SaveAsync(slotId);
        return true;
    }

    // ----------------------------
    // Pending Upload Flush
    // ----------------------------

    private async UniTaskVoid TryFlushPendingUpload(string slotId)
    {
        if (!remoteReady || remoteStorage == null) return;
        if (!IsPendingUpload(slotId)) return;

        try
        {
            var localBytes = await localStorage.LoadAsync(slotId);
            if (localBytes == null || localBytes.Length == 0) return;

            await remoteStorage.SaveAsync(slotId, localBytes);
            SetPendingUpload(slotId, false);
            Debug.Log($"[SaveManager] Pending upload flushed OK slot={slotId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Pending upload flush failed. {e}");
        }
    }

    private void OnApplicationPause(bool pause)
    {
#if UNITY_EDITOR
        return;
#else
        if (pause) AutoSaveService.I?.FlushAsync().Forget();
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

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.homeKey.wasPressedThisFrame)
        {
            ResetSlotAsync("slot1", saveDefaultImmediately: false).Forget();
        }

        //if (Keyboard.current.pKey.wasPressedThisFrame)
        //{
        //    TryFlushPendingUpload("slot1").Forget();
        //}
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private SaveGameDTO BuildSaveDTO(string slotId)
    {
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

        if (SettingManager.I != null)
            save.settings = SettingManager.I.Capture();

        return save;
    }

    private SaveGameDTO ChooseNewer(SaveGameDTO a, SaveGameDTO b)
    {
        if (a == null) return b;
        if (b == null) return a;

        if (a.meta.savedAtUtcTicks > b.meta.savedAtUtcTicks) return a;
        if (b.meta.savedAtUtcTicks > a.meta.savedAtUtcTicks) return b;

        return a;
    }

    private void SetPendingUpload(string slotId, bool pending)
    {
        PlayerPrefs.SetInt(PendingUploadKeyPrefix + slotId, pending ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool IsPendingUpload(string slotId)
    {
        return PlayerPrefs.GetInt(PendingUploadKeyPrefix + slotId, 0) == 1;
    }
}
