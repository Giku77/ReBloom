using System.Collections.Generic;
using UnityEngine;

public class GreenhouseFarmDroneSystem : MonoBehaviour
{
    [Header("Beds")]
    [SerializeField] private FarmBed[] beds;

    [Header("Storage")]
    [SerializeField] private WorldStorage targetStorage;
    [SerializeField] private int fertilizerItemId = FarmConst.FertilizerItemId;

    [Header("Harvest")]
    [SerializeField] private float harvestIntervalSeconds = 5f;
    [SerializeField] private int harvestPerTick = 1;

    [Header("Upgrade")]
    [SerializeField] private bool autoFertilize;
    [SerializeField] private float fertilizerDuration = FarmConst.FertilizerDuration;
    [SerializeField] private float fertilizeIntervalSeconds = 10f;

    private float _harvestTimer;
    private float _fertTimer;

    private readonly List<(int itemId, int count)> _dropBuffer = new();

    private void BuildActualDrops(FarmCropRowData row, List<(int itemId, int count)> outList)
    {
        outList.Clear();

        foreach (var d in row.drops)
        {
            if (d.itemId == 0 || d.count <= 0) continue;

            if (d.rate >= 1f || Random.value <= d.rate)
                outList.Add((d.itemId, d.count));
        }
    }


    private void Awake()
    {
        if (beds == null || beds.Length == 0)
            beds = GetComponentsInChildren<FarmBed>(true);

        if (targetStorage == null)
            targetStorage = GetComponentInChildren<WorldStorage>(true);
    }

    private float _nextFertilizeTime = -1f;

    public void SetAutoFertilize(bool on)
    {
        if (autoFertilize == on) return;

        autoFertilize = on;

        if (autoFertilize)
        {
            _nextFertilizeTime = Time.time; 
        }
    }


    private bool CanStoreAllDrops(StorageData storage, List<(int itemId, int count)> drops)
    {
        for (int i = 0; i < drops.Count; i++)
        {
            if (!storage.CanAddItem(drops[i].itemId, drops[i].count))
                return false;
        }
        return true;
    }


    private void OnEnable()
    {
        _harvestTimer = 0f;
        if (autoFertilize)
            _nextFertilizeTime = Time.time;
    }

    private void Update()
    {
        // 1) 비료 자동 (강화 켜진 경우만)
        if (autoFertilize && Time.time >= _nextFertilizeTime)
        {
            bool applied = TryAutoFertilize();

            _nextFertilizeTime = Time.time + (applied ? fertilizerDuration : 2f);
        }

        // 2) 자동 수확
        _harvestTimer += Time.deltaTime;
        if (_harvestTimer >= harvestIntervalSeconds)
        {
            _harvestTimer = 0f;
            TryAutoHarvest();
        }
    }

    private void StoreAllDrops(StorageData storage, List<(int itemId, int count)> drops)
    {
        for (int i = 0; i < drops.Count; i++)
            storage.AddItem(drops[i].itemId, drops[i].count);
    }


    private int CountActiveBeds()
    {
        int c = 0;
        for (int i = 0; i < beds.Length; i++)
            if (beds[i] != null && beds[i].gameObject.activeInHierarchy)
                c++;
        return c;
    }

    private bool TryAutoFertilize()
    {
        if (targetStorage == null) return false;
        var storage = targetStorage.GetStorageData();
        if (storage == null) return false;

        int activeBeds = CountActiveBeds();
        if (activeBeds <= 0) return false;

        int need = activeBeds;

        int have = storage.GetItemCount(fertilizerItemId);
        if (have < need) return false;

        if (!storage.TryRemoveItem(fertilizerItemId, need))
            return false;

        for (int b = 0; b < beds.Length; b++)
        {
            var bed = beds[b];
            if (bed == null || !bed.gameObject.activeInHierarchy) continue;

            for (int s = 0; s < bed.SlotCount; s++)
                bed.TryApplyFertilizer(s, fertilizerDuration);
        }

        return true;
    }



    private void TryAutoHarvest()
    {
        if (targetStorage == null) return;
        var storage = targetStorage.GetStorageData();
        if (storage == null) return;

        int done = 0;

        for (int b = 0; b < beds.Length && done < harvestPerTick; b++)
        {
            var bed = beds[b];
            if (bed == null || !bed.gameObject.activeInHierarchy) continue;

            for (int s = 0; s < bed.SlotCount && done < harvestPerTick; s++)
            {
                if (!bed.CanHarvest(s)) continue;
                if (!bed.TryHarvest(s, out var row)) continue;

                BuildActualDrops(row, _dropBuffer);

                if (_dropBuffer.Count > 0)
                {
                    if (!CanStoreAllDrops(storage, _dropBuffer))
                        continue;

                    StoreAllDrops(storage, _dropBuffer);
                }

                bed.TryHarvestInternal(s, out _);

                done++;
            }
        }
    }
}
