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
    [SerializeField] private float pendingRetryIntervalSeconds = 10f;

    public bool IsLoading { get; private set; }
    public bool HasLoadedOnce { get; private set; }
    public bool RemoteReady => remoteReady;

    private ISaveStorage localStorage;
    private ISaveStorage remoteStorage;

    private bool ready;
    private bool remoteReady;
    private bool remoteInitInProgress;
    private float nextPendingRetryTime;

    private const string PendingUploadKeyPrefix = "save_pending_upload_";
    private const string DefaultSlotId = "slot1";

    private enum SaveSource
    {
        None,
        Local,
        Remote
    }

    private sealed class LoadedSave
    {
        public SaveSource Source;
        public SaveGameDTO Save;
        public byte[] Bytes;

        public bool HasBytes => Bytes != null && Bytes.Length > 0;
        public bool IsValid => Save != null && HasBytes;
    }

    private async void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        localStorage = new LocalFileSaveStorage();

        remoteReady = false;
        remoteStorage = null;

        if (usePlayFab)
            await EnsureRemoteReadyAsync();

        ready = (localStorage != null);
    }

    private async UniTask WaitReadyAsync()
    {
        await UniTask.WaitUntil(() => ready && localStorage != null);
    }

    // ----------------------------
    // Public API
    // ----------------------------

    public async UniTask<bool> HasSaveAsync(string slotId = DefaultSlotId)
    {
        await WaitReadyAsync();

        if (await localStorage.ExistsAsync(slotId))
            return true;

        if (await EnsureRemoteReadyAsync() && remoteStorage != null)
        {
            try
            {
                if (await remoteStorage.ExistsAsync(slotId)) return true;
            }
            catch { /* ignore */ }
        }

        return false;
    }

    public async UniTask<bool> SaveAsync(string slotId = DefaultSlotId)
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

        if (await TryUploadRemoteAsync(slotId, bytes))
        {
            SetPendingUpload(slotId, false);
            Debug.Log($"[SaveAsync] Remote upload OK slot={slotId}");
        }
        else
        {
            SetPendingUpload(slotId, true);
        }

        Debug.Log($"[SaveAsync] slot={slotId} buildings={save.world.placedBuildings.Count} containers={save.world.containers.Count}");
        return true;
    }

    public async UniTask<bool> LoadAsync(string slotId = DefaultSlotId)
    {
        await WaitReadyAsync();

        IsLoading = true;
        try
        {
            // Local-first: load the local cache first, then reconcile it with remote if available.
            var local = await TryLoadSaveAsync(localStorage, slotId, SaveSource.Local);

            LoadedSave remote = null;
            if (await EnsureRemoteReadyAsync() && remoteStorage != null)
                remote = await TryLoadSaveAsync(remoteStorage, slotId, SaveSource.Remote);

            if ((local == null || !local.HasBytes) &&
                (remote == null || !remote.HasBytes))
            {
                Debug.LogWarning($"[LoadAsync] No data slot={slotId} (remote & local empty)");
                return false;
            }

            var chosen = ChooseNewer(local, remote);
            if (chosen == null || !chosen.IsValid)
            {
                Debug.LogWarning($"[LoadAsync] Both saves invalid slot={slotId}");
                return false;
            }

            await SyncChosenSaveAsync(slotId, local, chosen);

            var chosenSave = chosen.Save;

            if (SettingManager.I != null && chosenSave.settings != null)
                SettingManager.I.Apply(chosenSave.settings);

            if (!string.IsNullOrEmpty(chosenSave.meta.sceneName) &&
                SceneManager.GetActiveScene().name != chosenSave.meta.sceneName)
            {
                await SceneManager.LoadSceneAsync(chosenSave.meta.sceneName);
            }

            await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null);
            await UniTask.DelayFrame(1);

            var saveables = SaveRegistry.FindAllSaveablesInScene();
            foreach (var s in saveables)
                s.Restore(chosenSave);

            await UniTask.DelayFrame(1);

            var saveables2 = SaveRegistry.FindAllSaveablesInScene();
            foreach (var s in saveables2)
                if (s is not WorldBuildingsSaveable) s.Restore(chosenSave);

            HasLoadedOnce = true;
            AutoSaveService.I?.MarkClean();

            Debug.Log($"[LoadAsync] slot={slotId} source={chosen.Source} commit={chosenSave.meta.commitId} savedAt={chosenSave.meta.savedAtUtcTicks} (remoteReady={remoteReady})");

            if (IsPendingUpload(slotId))
                TryFlushPendingUpload(slotId).Forget();

            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async UniTask<bool> ResetSlotAsync(string slotId = DefaultSlotId, bool saveDefaultImmediately = true)
    {
        await WaitReadyAsync();

        try { await localStorage.DeleteAsync(slotId); } catch { }
        if (await EnsureRemoteReadyAsync() && remoteStorage != null)
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
        if (!IsPendingUpload(slotId)) return;
        if (!await EnsureRemoteReadyAsync() || remoteStorage == null) return;

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
            remoteReady = false;
            remoteStorage = null;
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
        SaveAsync(DefaultSlotId).Forget();
#endif
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextPendingRetryTime && IsPendingUpload(DefaultSlotId))
        {
            nextPendingRetryTime = Time.unscaledTime + Mathf.Max(1f, pendingRetryIntervalSeconds);
            TryFlushPendingUpload(DefaultSlotId).Forget();
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.homeKey.wasPressedThisFrame)
        {
            ResetSlotAsync(DefaultSlotId, saveDefaultImmediately: false).Forget();
        }

        //if (Keyboard.current.pKey.wasPressedThisFrame)
        //{
        //    TryFlushPendingUpload(DefaultSlotId).Forget();
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

    private LoadedSave ChooseNewer(LoadedSave local, LoadedSave remote)
    {
        if (local == null || !local.IsValid) return remote;
        if (remote == null || !remote.IsValid) return local;

        if (local.Save.meta.savedAtUtcTicks >= remote.Save.meta.savedAtUtcTicks) return local;
        return remote;
    }

    private async UniTask<bool> EnsureRemoteReadyAsync()
    {
        if (!usePlayFab) return false;
        if (remoteReady && remoteStorage != null) return true;

        if (remoteInitInProgress)
        {
            await UniTask.WaitUntil(() => !remoteInitInProgress);
            return remoteReady && remoteStorage != null;
        }

        remoteInitInProgress = true;
        try
        {
            await PlayFabAuth.LoginAsync();
            remoteStorage ??= new PlayFabSaveStorage();
            remoteReady = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Remote init failed. Local only. {e}");
            remoteReady = false;
            remoteStorage = null;
            return false;
        }
        finally
        {
            remoteInitInProgress = false;
        }
    }

    private async UniTask<LoadedSave> TryLoadSaveAsync(ISaveStorage storage, string slotId, SaveSource source)
    {
        if (storage == null)
            return null;

        var loaded = new LoadedSave { Source = source };

        try
        {
            loaded.Bytes = await storage.LoadAsync(slotId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadAsync] {source} load failed. {e}");
            return loaded;
        }

        if (!loaded.HasBytes)
            return loaded;

        try
        {
            loaded.Save = SaveSerializerNewtonsoft.FromBytes<SaveGameDTO>(loaded.Bytes, compressGzip);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadAsync] {source} deserialize failed. {e}");
        }

        return loaded;
    }

    private async UniTask SyncChosenSaveAsync(string slotId, LoadedSave local, LoadedSave chosen)
    {
        if (chosen == null || !chosen.IsValid)
            return;

        if (chosen.Source == SaveSource.Remote)
        {
            if (!HasSameCommit(local, chosen))
            {
                try
                {
                    await localStorage.SaveAsync(slotId, chosen.Bytes);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LoadAsync] Remote->Local sync failed. {e}");
                }
            }

            SetPendingUpload(slotId, false);
            return;
        }

        if (await TryUploadRemoteAsync(slotId, chosen.Bytes))
        {
            SetPendingUpload(slotId, false);
        }
        else
        {
            SetPendingUpload(slotId, true);
        }
    }

    private async UniTask<bool> TryUploadRemoteAsync(string slotId, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return false;

        if (!await EnsureRemoteReadyAsync() || remoteStorage == null)
            return false;

        try
        {
            await remoteStorage.SaveAsync(slotId, bytes);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Remote upload failed -> pending. {e}");
            remoteReady = false;
            remoteStorage = null;
            return false;
        }
    }

    private bool HasSameCommit(LoadedSave a, LoadedSave b)
    {
        if (a == null || b == null || a.Save == null || b.Save == null)
            return false;

        return a.Save.meta.commitId == b.Save.meta.commitId &&
               a.Save.meta.savedAtUtcTicks == b.Save.meta.savedAtUtcTicks;
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
