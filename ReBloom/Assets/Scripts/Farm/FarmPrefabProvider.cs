using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FarmPrefabProvider : MonoBehaviour
{
    public static FarmPrefabProvider I { get; private set; }

    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _cache = new();

    private readonly Dictionary<string, GreenhouseUpgradeState> _upgradeStateCache = new();

    public GreenhouseUpgradeState GetOrCreateUpgradeState(string greenhouseId)
    {
        if (!_upgradeStateCache.TryGetValue(greenhouseId, out var state) || state == null)
        {
            state = new GreenhouseUpgradeState { greenhouseId = greenhouseId };
            _upgradeStateCache[greenhouseId] = state;
        }
        return state;
    }

    private FarmDB _farmDB = new FarmDB();
    public FarmDB FarmDB => _farmDB;
    private GreenhouseUpgradeDB _greenhouseUpgradeDB = new GreenhouseUpgradeDB();
    public GreenhouseUpgradeDB GreenhouseUpgradeDB => _greenhouseUpgradeDB;
    private SeedPurifyDB _seedPurifyDB = new SeedPurifyDB();
    public SeedPurifyDB SeedPurifyDB => _seedPurifyDB;
    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        _farmDB.LoadFromBG();
        _greenhouseUpgradeDB.LoadFromBG();
        _seedPurifyDB.LoadFromBG();
    }

    public async UniTask<GameObject> LoadPrefabAsync(string address)
    {
        if (string.IsNullOrEmpty(address)) return null;

        if (_cache.TryGetValue(address, out var h))
        {
            if (h.IsValid() && h.Status == AsyncOperationStatus.Succeeded)
                return h.Result;

            // 실패/깨진 핸들은 제거
            if (h.IsValid()) Addressables.Release(h);
            _cache.Remove(address);
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.ToUniTask();

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[FarmPrefabProvider] Load failed: {address}");
            return null;
        }

        _cache[address] = handle;
        return handle.Result;
    }

    public async UniTask<GameObject> InstantiateAsync(string address, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        // InstantiateAsync로 바로 해도 되는데,
        // 여기선 Load 캐시를 타고 Instantiate 하도록 구성
        var prefab = await LoadPrefabAsync(address);
        if (prefab == null) return null;

        return Instantiate(prefab, pos, rot, parent);
    }

    public void ReleaseInstance(GameObject inst)
    {
        if (inst == null) return;
        // Addressables.InstantiateAsync를 쓰는 방식이면 ReleaseInstance가 정석이고,
        // 위처럼 Instantiate(prefab)라면 Destroy()가 맞음.
        // (아래 2번에서 "Addressables.InstantiateAsync" 방식으로 통일 추천)
        Destroy(inst);
    }

    private void OnDestroy()
    {
        foreach (var kv in _cache)
        {
            if (kv.Value.IsValid())
                Addressables.Release(kv.Value);
        }
        _cache.Clear();
    }

    public async UniTask<GameObject> InstantiateAddressableAsync(string address, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (string.IsNullOrEmpty(address)) return null;

        var h = Addressables.InstantiateAsync(address, pos, rot, parent);
        var go = await h.ToUniTask();

        if (h.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[FarmPrefabProvider] Instantiate failed: {address}");
            return null;
        }

        return go;
    }

    public void ReleaseAddressableInstance(GameObject inst)
    {
        if (inst == null) return;
        Addressables.ReleaseInstance(inst);
    }
}
