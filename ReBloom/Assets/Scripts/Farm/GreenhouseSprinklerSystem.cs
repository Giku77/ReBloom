using UnityEngine;

public class GreenhouseSprinklerSystem : MonoBehaviour
{
    [Header("Beds")]
    [SerializeField] private FarmBed[] beds;

    [Header("Sprinkler")]
    [SerializeField] private float intervalSeconds = 10f;
    [SerializeField] private int waterPerTick = 1;

    // 물 1회 급수당 탱크에서 몇 % 소비할지(밸런스)
    [SerializeField] private int waterCostPerWaterAction = 1;

    private float _timer;

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
        _timer += Time.deltaTime;
        if (_timer < intervalSeconds) return;
        _timer = 0f;

        var tank = WaterTankService.I?.Manager;
        if (tank == null) return;                // 물탱크 시스템 자체가 없음
        if (tank.WaterLevel <= 0) return;        // 물이 없음

        // 1) 이번 틱에 "실제로 물이 필요한 횟수" 계산
        int neededActions = 0;

        for (int b = 0; b < beds.Length; b++)
        {
            var bed = beds[b];
            if (bed == null || !bed.gameObject.activeInHierarchy) continue;

            for (int i = 0; i < bed.SlotCount; i++)
            {
                // waterPerTick만큼 줄 수 있지만, CanWater가 true일 때만 필요함
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

        // 2) 물 충분한지 체크 + 소비
        int cost = neededActions * waterCostPerWaterAction;
        if (!tank.TryConsumeWater(cost))
        {
            // 물 부족이면 아예 안 주거나(현재), 가능한 만큼만 줄 수도 있음
            return;
        }

        // 3) 실제 급수 적용
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
