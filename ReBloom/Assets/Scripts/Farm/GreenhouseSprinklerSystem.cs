using Unity.Netcode;
using UnityEngine;

public class GreenhouseSprinklerSystem : MonoBehaviour
{
    [Header("Beds")]
    [SerializeField] private FarmBed[] beds;

    [Header("Sprinkler")]
    [SerializeField] private float intervalSeconds = 10f;
    [SerializeField] private int waterPerTick = 1;

    [SerializeField] private int waterCostPerWaterAction = 1;

    private float _timer;

    private bool IsNetworkSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    private bool HasServerAuthority => !IsNetworkSession || NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        if (beds == null || beds.Length == 0)
            beds = GetComponentsInChildren<FarmBed>(true);
    }

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        if (!HasServerAuthority)
            return;

        _timer += Time.deltaTime;
        if (_timer < intervalSeconds) return;
        _timer = 0f;

        var tank = WaterTankService.I?.FindClosestTank(transform.position);
        if (tank == null) return;
        if (tank.WaterLevel <= 0) return;

        int neededActions = 0;

        for (int b = 0; b < beds.Length; b++)
        {
            var bed = beds[b];
            if (bed == null || !bed.gameObject.activeInHierarchy) continue;

            for (int i = 0; i < bed.SlotCount; i++)
            {
                int can = 0;
                for (int k = 0; k < waterPerTick; k++)
                {
                    if (bed.CanWater(i)) can++;
                    else break;
                }
                neededActions += can;
            }
        }

        if (neededActions <= 0) return;

        int cost = neededActions * waterCostPerWaterAction;
        if (!tank.TryConsumeWater(cost))
            return;

        for (int b = 0; b < beds.Length; b++)
        {
            var bed = beds[b];
            if (bed == null || !bed.gameObject.activeInHierarchy) continue;

            for (int i = 0; i < bed.SlotCount; i++)
            {
                for (int k = 0; k < waterPerTick; k++)
                {
                    if (bed.CanWater(i)) bed.Water(i);
                    else break;
                }
            }
        }
    }
}
