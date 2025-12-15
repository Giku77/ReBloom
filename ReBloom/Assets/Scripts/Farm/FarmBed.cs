using Cysharp.Threading.Tasks;
using UnityEngine;

public class FarmBed : MonoBehaviour
{
    [SerializeField] private Transform[] slotPoints; // 인스펙터에서 8개 할당

    [SerializeField] private FarmSlotHighlight[] slotHighlights;
    [SerializeField] private CropSlot[] slots = new CropSlot[8];

    public CropSlot[] Slots => slots;

    public int SlotCount => slots.Length;

    public CropSlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    private FarmDB farmDB;
    public FarmDB FarmDB => farmDB;

    public event System.Action OnChanged;

    private System.Threading.CancellationTokenSource[] _slotCts;

    private void Awake()
    {
        // 빈 슬롯 초기화
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new CropSlot();
        }
        _slotCts = new System.Threading.CancellationTokenSource[slots.Length];
        farmDB = FarmPrefabProvider.I.FarmDB;
    }

    private void Update()
    {
        // 임시: 나중에 FarmingManager로 옮겨도 됨
        float dt = Time.deltaTime;
        for (int i = 0; i < slots.Length; i++)
        {
            TickSlot(i, dt);
        }
    }

    private void TickSlot(int index, float dt)
    {
        var slot = slots[index];
        if (slot.state != CropSlotState.Growing) return;
        if (!farmDB.TryGet(slot.cropId, out var row)) return;

        var stage = row.stages[slot.stageIndex];

        // 단계별 물 요구: 물이 부족하면 시간 진행 안함(너가 원하면 '말라죽기'로 바꿔도 됨)
        if (stage.needWater > 0 && slot.wateredCount < stage.needWater)
            return;

        slot.stageTimer += dt;

        // needTime 단위가 테이블에서 864, 1728 이런 값이면
        // "초인지/분인지/게임틱인지" 기준을 하나 정해야 함 (지금은 dt가 Time.deltaTime 기준이므로 초라고 가정)
        if (slot.stageTimer >= stage.needTime)
        {
            slot.stageTimer = 0f;
            slot.wateredCount = 0;
            slot.stageIndex++;

            // 마지막 stage는 “최종 비주얼”만 존재한다고 했으니 Mature 처리
            if (slot.stageIndex >= row.stages.Length - 1)
                slot.state = CropSlotState.Mature;

            UpdateSlotVisual(index);
            OnChanged?.Invoke();
        }
    }
    private async UniTaskVoid UpdateSlotVisualAsync(int index)
    {
        // 이전 로딩 취소
        _slotCts[index]?.Cancel();
        _slotCts[index]?.Dispose();
        _slotCts[index] = new System.Threading.CancellationTokenSource();
        var token = _slotCts[index].Token;

        var slot = slots[index];
        var point = slotPoints[index];

        // 기존 비주얼 정리 (Addressables InstantiateAsync를 쓸 경우 ReleaseInstance!)
        if (slot.visual != null)
        {
            FarmPrefabProvider.I.ReleaseAddressableInstance(slot.visual.gameObject);
            slot.visual = null;
        }

        if (slot.state == CropSlotState.Empty) return;
        if (!farmDB.TryGet(slot.cropId, out var row)) return;

        string prefabKey = null;

        if (slot.state == CropSlotState.Growing)
            prefabKey = row.stages[slot.stageIndex].prefabKey;
        else if (slot.state == CropSlotState.Mature)
            prefabKey = row.stages[row.stages.Length - 1].prefabKey;

        if (string.IsNullOrEmpty(prefabKey)) return;

        // Addressables instantiate
        var go = await FarmPrefabProvider.I.InstantiateAddressableAsync(prefabKey, point.position, point.rotation, point);
        if (token.IsCancellationRequested)
        {
            if (go != null) FarmPrefabProvider.I.ReleaseAddressableInstance(go);
            return;
        }

        slot.visual = go != null ? go.GetComponent<CropVisual>() : null;
    }

    // 단계 변경 시 여기 호출
    private void UpdateSlotVisual(int index)
    {
        UpdateSlotVisualAsync(index).Forget();
    }


    public void SetSlotHighlighted(int index, bool on)
    {
        if (index < 0 || index >= slotHighlights.Length) return;
        var h = slotHighlights[index];
        if (h != null)
            h.SetHighlighted(on);

        var slot = slots[index];
        if (slot.visual != null)
            slot.visual.SetHighlighted(on);
    }

    // ====== 외부에서 호출할 API들 ======

    public bool CanPlant(int index, int cropId)
    {
        if (index < 0 || index >= slots.Length) return false;
        return slots[index].state == CropSlotState.Empty && cropId != 0;
    }

    public void Plant(int index, int cropId)
    {
        var slot = slots[index];
        slot.state = CropSlotState.Growing;
        slot.cropId = cropId;
        slot.stageIndex = 0;
        slot.stageTimer = 0;
        slot.wateredCount = 0;

        UpdateSlotVisual(index);
        OnChanged?.Invoke();
    }


    public bool CanWater(int index)
    {
        var slot = slots[index];
        if (slot.state != CropSlotState.Growing) return false;
        if (!farmDB.TryGet(slot.cropId, out var row)) return false;

        var stage = row.stages[slot.stageIndex];
        return stage.needWater > 0 && slot.wateredCount < stage.needWater;
    }

    public void Water(int index)
    {
        slots[index].wateredCount++;
        OnChanged?.Invoke();
    }


    public bool CanHarvest(int index)
    {
        var slot = slots[index];
        return slot.state == CropSlotState.Mature;
    }

    public bool TryHarvest(int index, out FarmCropRowData row)
    {
        row = null;
        var slot = slots[index];
        if (slot.state != CropSlotState.Mature) return false;
        if (!farmDB.TryGet(slot.cropId, out row)) return false;
        return true;
    }

    public void Harvest(int index, PlayerController player)
    {
        var slot = slots[index];
        if (slot.state != CropSlotState.Mature) return;
        if (!farmDB.TryGet(slot.cropId, out var row)) return;

        foreach (var d in row.drops)
        {
            if (d.rate >= 1f || UnityEngine.Random.value <= d.rate)
                player.Inventory.AddItem(d.itemId, d.count);
        }

        // 슬롯 초기화
        slot.state = CropSlotState.Empty;
        slot.cropId = 0;
        slot.stageIndex = 0;
        slot.stageTimer = 0;
        slot.wateredCount = 0;

        UpdateSlotVisual(index);
        OnChanged?.Invoke();
    }

    public void Uproot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        var slot = slots[index];
        if (slot.state == CropSlotState.Empty) return;

        slot.state = CropSlotState.Empty;
        slot.cropId = 0;
        slot.stageIndex = 0;
        slot.stageTimer = 0;
        slot.wateredCount = 0;

        UpdateSlotVisual(index);
        OnChanged?.Invoke();
    }
}
