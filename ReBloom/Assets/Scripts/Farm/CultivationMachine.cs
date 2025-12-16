using UnityEngine;

public class CultivationMachine : MonoBehaviour
{
    [SerializeField] private CultivationSlot slot = new CultivationSlot();

    private FarmDB farmDB;
    public CultivationSlot Slot => slot;

    public event System.Action OnChanged;

    private void Awake()
    {
        farmDB = FarmPrefabProvider.I.FarmDB;
        if (slot == null) slot = new CultivationSlot();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float dt)
    {
        if (slot.state != CultivationSlotState.Running) return;

        if (!HasEnoughPower(slot.requiredPowerKw))
            return;

        slot.remainTime -= dt;
        if (slot.remainTime > 0f) return;

        slot.remainTime = 0f;
        slot.state = CultivationSlotState.ReadyToCollect;
        OnChanged?.Invoke();
    }

    // ===== 외부 API (index 제거) =====

    public bool CanStart(int seedItemId, out string reason)
    {
        reason = null;

        if (slot.state != CultivationSlotState.Empty)
        {
            reason = "이미 사용 중입니다.";
            return false;
        }

        if (!farmDB.TryGetBySeedId(seedItemId, out var cropRow))
        {
            reason = "배양할 수 없는 아이템입니다.";
            return false;
        }

        float reqKw = GetRequiredPowerKw(cropRow.cropId);
        if (!HasEnoughPower(reqKw))
        {
            reason = "전력이 부족합니다.";
            return false;
        }

        return true;
    }

    public void DebugForceReadyToCollect()
    {
        if (slot == null) slot = new CultivationSlot();

        if (slot.outputItemId <= 0 || slot.outputCount <= 0)
        {
            slot.outputItemId = 4005004; // TODO: 테스트 아이템 ID
            slot.outputCount = 1;
        }

        slot.remainTime = 0f;
        slot.state = CultivationSlotState.ReadyToCollect;

        OnChanged?.Invoke();
    }

    public bool StartMachine(int seedItemId)
    {
        if (slot.state != CultivationSlotState.Empty) return false;
        if (!farmDB.TryGetBySeedId(seedItemId, out var cropRow)) return false;

        float totalTime = ComputeTotalTimeSeconds(cropRow);

        int outItemId = 0;
        int outCount = 0;
        if (cropRow.drops != null && cropRow.drops.Length > 0)
        {
            outItemId = cropRow.drops[0].itemId;
            outCount = cropRow.drops[0].count;
        }

        float reqKw = GetRequiredPowerKw(cropRow.cropId);

        slot.seedItemId = seedItemId;
        slot.state = CultivationSlotState.Running;
        slot.cropId = cropRow.cropId;
        slot.remainTime = totalTime;
        slot.outputItemId = outItemId;
        slot.outputCount = outCount;
        slot.requiredPowerKw = reqKw;

        OnChanged?.Invoke();
        return true;
    }

    public bool CanCollect() => slot.state == CultivationSlotState.ReadyToCollect;

    public bool Collect(PlayerController player, out string reason)
    {
        reason = null;
        if (player == null) { reason = "플레이어가 없습니다."; return false; }

        if (slot.state != CultivationSlotState.ReadyToCollect)
        {
            reason = "수거할 수 없습니다.";
            return false;
        }

        if (slot.outputItemId <= 0 || slot.outputCount <= 0)
        {
            reason = "수거할 아이템이 없습니다.";
            return false;
        }

        player.Inventory.AddItem(slot.outputItemId, slot.outputCount);

        slot = new CultivationSlot(); // 초기화
        OnChanged?.Invoke();
        return true;
    }

    // ===== 전력/시간 =====

    private float GetRequiredPowerKw(int cropId)
    {
        BuildManager.I.ArcDB.TryGet(3101008, out var arcData);
        return arcData != null ? arcData.energyDec : 12.345f;
    }

    private bool HasEnoughPower(float requiredKw)
    {
        // TODO: 실제 가용 전력 체크로 교체
        return true;
    }

    private float ComputeTotalTimeSeconds(FarmCropRowData row)
    {
        if (row.stages == null || row.stages.Length == 0) return 0f;

        float sum = 0f;
        int lastGrowStage = Mathf.Max(0, row.stages.Length - 1);
        for (int i = 0; i < lastGrowStage; i++)
            sum += row.stages[i].needTime;

        return sum;
    }
}
