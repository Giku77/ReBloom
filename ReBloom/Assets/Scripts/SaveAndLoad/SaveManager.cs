using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I { get; private set; }

    private const string DefaultSlotId = "slot1";
    private const string SlotIndexStorageId = "__world_slot_index";
    private const string PendingUploadKeyPrefix = "save_pending_upload_";

    [Header("Serialize")]
    [SerializeField] private bool compressGzip = true;

    [Header("Remote")]
    [SerializeField] private bool usePlayFab = true;

    public bool IsLoading { get; private set; }
    public bool HasLoadedOnce { get; private set; }
    public bool RemoteReady => remoteReady;
    public string ActiveSlotId { get; private set; } = DefaultSlotId;
    public string ActiveSlotDisplayName { get; private set; } = string.Empty;

    private ISaveStorage localStorage;
    private PlayFabSaveStorage playFabStorage;
    private ISaveStorage remoteStorage;

    private bool ready;
    private bool remoteReady;

    private async void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        localStorage = new LocalFileSaveStorage();
        ApplySelectedSlotFromContext();

        remoteReady = false;
        remoteStorage = null;
        playFabStorage = null;

        if (usePlayFab && PlayFabAuth.HasCredentialInput)
        {
            await EnsureRemoteReadyAsync();
        }

        ready = localStorage != null;
    }

    private async UniTask WaitReadyAsync()
    {
        await UniTask.WaitUntil(() => ready && localStorage != null);
    }
    public async UniTask<bool> EnsureRemoteReadyAsync(string credentialInput = null)
    {
        await UniTask.WaitUntil(() => localStorage != null);

        if (!usePlayFab)
            return false;

        try
        {
            await PlayFabAuth.LoginAsync(credentialInput);
            playFabStorage ??= new PlayFabSaveStorage();
            remoteStorage = playFabStorage;
            remoteReady = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Remote init/login failed. Local only. {e.Message}");
            remoteReady = false;
            remoteStorage = null;
            return false;
        }
    }
    public async UniTask<bool> EnsureRemoteReadyAsync(string displayName, string password)
    {
        await UniTask.WaitUntil(() => localStorage != null);

        if (!usePlayFab)
            return false;

        try
        {
            await PlayFabAuth.LoginAsync(displayName, password);
            playFabStorage ??= new PlayFabSaveStorage();
            remoteStorage = playFabStorage;
            remoteReady = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Remote init/login failed. Local only. {e.Message}");
            remoteReady = false;
            remoteStorage = null;
            return false;
        }
    }

    public void SetActiveSlot(string slotId, string displayName = null)
    {
        ActiveSlotId = NormalizeSlotId(slotId);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            ActiveSlotDisplayName = displayName.Trim();
        }
        else if (ActiveSlotId == NormalizeSlotId(GameStartContext.SlotId) && !string.IsNullOrWhiteSpace(GameStartContext.SlotDisplayName))
        {
            ActiveSlotDisplayName = GameStartContext.SlotDisplayName.Trim();
        }
        else
        {
            ActiveSlotDisplayName = ActiveSlotId;
        }
        GameStartContext.SlotId = ActiveSlotId;
        GameStartContext.SlotDisplayName = ActiveSlotDisplayName;
    }

    public async UniTask<IReadOnlyList<WorldSlotMetaDTO>> ListWorldSlotsAsync()
    {
        await WaitReadyAsync();
        var index = await LoadSlotIndexAsync();
        return index.slots
            .OrderByDescending(slot => slot.lastSavedAtUtcTicks)
            .ToArray();
    }

    public async UniTask<WorldSlotMetaDTO> GetWorldSlotMetaAsync(string slotId = null)
    {
        await WaitReadyAsync();
        var resolved = ResolveSlotId(slotId);
        var index = await LoadSlotIndexAsync();
        return index.slots.FirstOrDefault(slot => slot.slotId == resolved);
    }

    public async UniTask<string> SuggestNextSlotIdAsync(int maxSlots = 8)
    {
        var metas = await ListWorldSlotsAsync();
        var existing = new HashSet<string>(metas.Select(meta => NormalizeSlotId(meta.slotId)));

        for (int i = 1; i <= maxSlots; i++)
        {
            string candidate = $"slot{i}";
            if (!existing.Contains(candidate))
                return candidate;
        }

        return $"slot{Mathf.Max(1, existing.Count + 1)}";
    }

    public async UniTask<bool> HasSaveAsync(string slotId = null)
    {
        await WaitReadyAsync();
        string resolved = ResolveSlotId(slotId);

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                if (await remoteStorage.ExistsAsync(resolved))
                    return true;
            }
            catch
            {
            }
        }

        return await localStorage.ExistsAsync(resolved);
    }

    public async UniTask<bool> SaveAsync(string slotId = null)
    {
        await WaitReadyAsync();

        if (IsLoading)
        {
            Debug.LogWarning($"[SaveAsync] blocked IsLoading={IsLoading} HasLoadedOnce={HasLoadedOnce}");
            return false;
        }

        string resolved = ResolveSlotId(slotId);
        var existingMeta = await GetWorldSlotMetaAsync(resolved);
        var save = BuildSaveDTO(resolved, existingMeta);

        var saveables = SaveRegistry.FindAllSaveablesInScene();
        foreach (var saveable in saveables)
            saveable.Capture(save);

        var bytes = SaveSerializerNewtonsoft.ToBytes(save, compressGzip);
        await localStorage.SaveAsync(resolved, bytes);

        bool remoteSaveFailed = false;
        if (remoteReady && remoteStorage != null)
        {
            try
            {
                await remoteStorage.SaveAsync(resolved, bytes);
                SetPendingUpload(resolved, false);
            }
            catch (Exception e)
            {
                remoteSaveFailed = true;
                Debug.LogWarning($"[SaveAsync] Remote upload failed -> pending. {e}");
                SetPendingUpload(resolved, true);
            }
        }
        else
        {
            remoteSaveFailed = true;
            SetPendingUpload(resolved, true);
        }

        var meta = BuildWorldSlotMeta(save, existingMeta);
        await UpsertSlotMetaAsync(meta, remoteSaveFailed);

        Debug.Log($"[SaveAsync] slot={resolved} buildings={save.world.placedBuildings.Count} containers={save.world.containers.Count}");
        return true;
    }

    public async UniTask<bool> LoadAsync(string slotId = null)
    {
        await WaitReadyAsync();

        string resolved = ResolveSlotId(slotId);
        SetActiveSlot(resolved);

        IsLoading = true;
        try
        {
            byte[] remoteBytes = null;
            byte[] localBytes = null;

            if (remoteReady && remoteStorage != null)
            {
                try { remoteBytes = await remoteStorage.LoadAsync(resolved); }
                catch (Exception e) { Debug.LogWarning($"[LoadAsync] Remote load failed. {e}"); }
            }

            try { localBytes = await localStorage.LoadAsync(resolved); }
            catch (Exception e) { Debug.LogWarning($"[LoadAsync] Local load failed. {e}"); }

            if ((remoteBytes == null || remoteBytes.Length == 0) &&
                (localBytes == null || localBytes.Length == 0))
            {
                Debug.LogWarning($"[LoadAsync] No data slot={resolved} (remote & local empty)");
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
                Debug.LogWarning($"[LoadAsync] Both saves invalid slot={resolved}");
                return false;
            }

            SetActiveSlot(resolved, chosen.meta.displayName);

            if (chosen == localSave && remoteReady && remoteStorage != null)
                SetPendingUpload(resolved, true);

            if (SettingManager.I != null && chosen.settings != null)
                SettingManager.I.Apply(chosen.settings);

            if (!string.IsNullOrEmpty(chosen.meta.sceneName) &&
                SceneManager.GetActiveScene().name != chosen.meta.sceneName)
            {
                bool isNetworkSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
                if (isNetworkSession)
                {
                    Debug.LogWarning($"[SaveManager] Skipping local scene load during network session. active={SceneManager.GetActiveScene().name} saved={chosen.meta.sceneName}");
                }
                else
                {
                    await SceneManager.LoadSceneAsync(chosen.meta.sceneName);
                }
            }

            await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.ArcDB != null);
            await UniTask.DelayFrame(1);

            var saveables = SaveRegistry.FindAllSaveablesInScene();
            foreach (var saveable in saveables)
                saveable.Restore(chosen);

            await UniTask.DelayFrame(1);

            var secondPassSaveables = SaveRegistry.FindAllSaveablesInScene();
            foreach (var saveable in secondPassSaveables)
                if (saveable is not WorldBuildingsSaveable) saveable.Restore(chosen);

            HasLoadedOnce = true;
            AutoSaveService.I?.MarkClean();

            await UpsertSlotMetaAsync(BuildWorldSlotMeta(chosen), remoteSave == null && remoteReady);
            TryFlushPendingUpload(resolved).Forget();
            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async UniTask<bool> ResetSlotAsync(string slotId = null, bool saveDefaultImmediately = true)
    {
        await WaitReadyAsync();

        string resolved = ResolveSlotId(slotId);
        SetActiveSlot(resolved);

        try { await localStorage.DeleteAsync(resolved); } catch { }
        if (remoteReady && remoteStorage != null)
        {
            try { await remoteStorage.DeleteAsync(resolved); } catch { }
        }

        await RemoveSlotMetaAsync(resolved);
        SetPendingUpload(resolved, false);

        if (!saveDefaultImmediately)
            return true;

        await SaveAsync(resolved);
        return true;
    }

    private async UniTaskVoid TryFlushPendingUpload(string slotId)
    {
        if (!remoteReady || remoteStorage == null) return;
        if (!IsPendingUpload(slotId)) return;

        try
        {
            var localBytes = await localStorage.LoadAsync(slotId);
            if (localBytes == null || localBytes.Length == 0)
                return;

            await remoteStorage.SaveAsync(slotId, localBytes);
            await FlushSlotIndexAsync();
            SetPendingUpload(slotId, false);
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
        SaveAsync(ActiveSlotId).Forget();
#endif
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.homeKey.wasPressedThisFrame)
        {
            ResetSlotAsync(ActiveSlotId, saveDefaultImmediately: false).Forget();
        }
    }

    private SaveGameDTO BuildSaveDTO(string slotId, WorldSlotMetaDTO existingMeta)
    {
        long now = DateTime.UtcNow.Ticks;
        string resolvedDisplayName = ResolveDisplayName(slotId, existingMeta);

        var save = new SaveGameDTO
        {
            meta =
            {
                slotId = slotId,
                displayName = resolvedDisplayName,
                hostPlayFabId = string.IsNullOrWhiteSpace(PlayFabAuth.CurrentPlayFabId) ? SystemInfo.deviceUniqueIdentifier : PlayFabAuth.CurrentPlayFabId,
                createdAtUtcTicks = existingMeta != null && existingMeta.createdAtUtcTicks > 0 ? existingMeta.createdAtUtcTicks : now,
                version = SaveConstants.SAVE_VERSION,
                sceneName = SceneManager.GetActiveScene().name,
                savedAtUtcTicks = now,
                commitId = Guid.NewGuid().ToString("N")
            }
        };

        if (SettingManager.I != null)
            save.settings = SettingManager.I.Capture();

        return save;
    }

    private WorldSlotMetaDTO BuildWorldSlotMeta(SaveGameDTO save, WorldSlotMetaDTO existingMeta = null)
    {
        if (save == null)
            return null;

        return new WorldSlotMetaDTO
        {
            slotId = NormalizeSlotId(save.meta.slotId),
            displayName = ResolveDisplayName(save.meta.slotId, existingMeta, save.meta.displayName),
            hostPlayFabId = string.IsNullOrWhiteSpace(save.meta.hostPlayFabId) ? (string.IsNullOrWhiteSpace(PlayFabAuth.CurrentPlayFabId) ? SystemInfo.deviceUniqueIdentifier : PlayFabAuth.CurrentPlayFabId) : save.meta.hostPlayFabId,
            sceneName = save.meta.sceneName,
            commitId = save.meta.commitId,
            createdAtUtcTicks = save.meta.createdAtUtcTicks > 0 ? save.meta.createdAtUtcTicks : existingMeta?.createdAtUtcTicks ?? DateTime.UtcNow.Ticks,
            lastSavedAtUtcTicks = save.meta.savedAtUtcTicks
        };
    }

    private async UniTask<SaveSlotIndexDTO> LoadSlotIndexAsync()
    {
        SaveSlotIndexDTO remoteIndex = null;
        SaveSlotIndexDTO localIndex = null;

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                remoteIndex = playFabStorage != null
                    ? await playFabStorage.LoadWorldSlotIndexAsync(compressGzip)
                    : await LoadSlotIndexFromStorageAsync(remoteStorage);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Remote slot index load failed. {e}");
            }
        }

        try
        {
            localIndex = await LoadSlotIndexFromStorageAsync(localStorage);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Local slot index load failed. {e}");
        }

        return ChooseNewerIndex(remoteIndex, localIndex);
    }

    private async UniTask<SaveSlotIndexDTO> LoadSlotIndexFromStorageAsync(ISaveStorage storage)
    {
        if (storage == null)
            return new SaveSlotIndexDTO();

        var bytes = await storage.LoadAsync(SlotIndexStorageId);
        if (bytes == null || bytes.Length == 0)
            return new SaveSlotIndexDTO();

        var index = SaveSerializerNewtonsoft.FromBytes<SaveSlotIndexDTO>(bytes, compressGzip);
        return NormalizeSlotIndex(index);
    }

    private async UniTask UpsertSlotMetaAsync(WorldSlotMetaDTO meta, bool remoteFailed)
    {
        if (meta == null)
            return;

        var index = await LoadSlotIndexAsync();
        index.updatedAtUtcTicks = DateTime.UtcNow.Ticks;

        index.slots.RemoveAll(slot => string.Equals(slot.slotId, meta.slotId, StringComparison.OrdinalIgnoreCase));
        index.slots.Add(meta);
        index = NormalizeSlotIndex(index);

        var bytes = SaveSerializerNewtonsoft.ToBytes(index, compressGzip);
        await localStorage.SaveAsync(SlotIndexStorageId, bytes);

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                if (playFabStorage != null)
                    await playFabStorage.SaveWorldSlotIndexAsync(index, compressGzip);
                else
                    await remoteStorage.SaveAsync(SlotIndexStorageId, bytes);
            }
            catch (Exception e)
            {
                remoteFailed = true;
                Debug.LogWarning($"[SaveManager] Remote slot index save failed. {e}");
            }
        }

        if (remoteFailed)
            SetPendingUpload(meta.slotId, true);
    }

    private async UniTask RemoveSlotMetaAsync(string slotId)
    {
        var index = await LoadSlotIndexAsync();
        index.slots.RemoveAll(slot => string.Equals(slot.slotId, slotId, StringComparison.OrdinalIgnoreCase));
        index.updatedAtUtcTicks = DateTime.UtcNow.Ticks;
        index = NormalizeSlotIndex(index);

        var bytes = SaveSerializerNewtonsoft.ToBytes(index, compressGzip);
        await localStorage.SaveAsync(SlotIndexStorageId, bytes);

        if (remoteReady && remoteStorage != null)
        {
            try
            {
                if (playFabStorage != null)
                    await playFabStorage.SaveWorldSlotIndexAsync(index, compressGzip);
                else
                    await remoteStorage.SaveAsync(SlotIndexStorageId, bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Remote slot index remove failed. {e}");
                SetPendingUpload(slotId, true);
            }
        }
    }

    private async UniTask FlushSlotIndexAsync()
    {
        if (!remoteReady || remoteStorage == null)
            return;

        var localBytes = await localStorage.LoadAsync(SlotIndexStorageId);
        if (localBytes == null || localBytes.Length == 0)
            return;

        if (playFabStorage != null)
        {
            var index = SaveSerializerNewtonsoft.FromBytes<SaveSlotIndexDTO>(localBytes, compressGzip) ?? new SaveSlotIndexDTO();
            await playFabStorage.SaveWorldSlotIndexAsync(index, compressGzip);
        }
        else
        {
            await remoteStorage.SaveAsync(SlotIndexStorageId, localBytes);
        }
    }

    private void ApplySelectedSlotFromContext()
    {
        ActiveSlotId = NormalizeSlotId(GameStartContext.SlotId);
        ActiveSlotDisplayName = string.IsNullOrWhiteSpace(GameStartContext.SlotDisplayName)
            ? ActiveSlotId
            : GameStartContext.SlotDisplayName.Trim();
    }

    private string ResolveSlotId(string slotId)
    {
        string candidate = string.IsNullOrWhiteSpace(slotId) ? ActiveSlotId : slotId;
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = GameStartContext.SlotId;
        return NormalizeSlotId(candidate);
    }

    private string NormalizeSlotId(string slotId)
    {
        return string.IsNullOrWhiteSpace(slotId) ? DefaultSlotId : slotId.Trim();
    }

    private string ResolveDisplayName(string slotId, WorldSlotMetaDTO existingMeta, string explicitDisplayName = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitDisplayName))
            return explicitDisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(ActiveSlotDisplayName) && ResolveSlotId(slotId) == ActiveSlotId)
            return ActiveSlotDisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(GameStartContext.SlotDisplayName) && ResolveSlotId(slotId) == NormalizeSlotId(GameStartContext.SlotId))
            return GameStartContext.SlotDisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(existingMeta?.displayName))
            return existingMeta.displayName.Trim();
        return NormalizeSlotId(slotId);
    }

    private SaveGameDTO ChooseNewer(SaveGameDTO a, SaveGameDTO b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return a.meta.savedAtUtcTicks >= b.meta.savedAtUtcTicks ? a : b;
    }

    private SaveSlotIndexDTO ChooseNewerIndex(SaveSlotIndexDTO a, SaveSlotIndexDTO b)
    {
        if (a == null || a.slots == null) return NormalizeSlotIndex(b);
        if (b == null || b.slots == null) return NormalizeSlotIndex(a);
        return a.updatedAtUtcTicks >= b.updatedAtUtcTicks ? NormalizeSlotIndex(a) : NormalizeSlotIndex(b);
    }

    private SaveSlotIndexDTO NormalizeSlotIndex(SaveSlotIndexDTO index)
    {
        index ??= new SaveSlotIndexDTO();
        index.slots ??= new List<WorldSlotMetaDTO>();

        index.slots = index.slots
            .Where(slot => slot != null && !string.IsNullOrWhiteSpace(slot.slotId))
            .GroupBy(slot => NormalizeSlotId(slot.slotId), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(slot => slot.lastSavedAtUtcTicks).First())
            .OrderByDescending(slot => slot.lastSavedAtUtcTicks)
            .ToList();

        return index;
    }

    private void SetPendingUpload(string slotId, bool pending)
    {
        PlayerPrefs.SetInt(PendingUploadKeyPrefix + NormalizeSlotId(slotId), pending ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool IsPendingUpload(string slotId)
    {
        return PlayerPrefs.GetInt(PendingUploadKeyPrefix + NormalizeSlotId(slotId), 0) == 1;
    }
}







