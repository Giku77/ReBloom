using UnityEngine;

public class FarmBed : MonoBehaviour
{
    [SerializeField] private Transform[] slotPoints; // 인스펙터에서 8개 할당

    [SerializeField] private FarmSlotHighlight[] slotHighlights;
    [SerializeField] private CropSlot[] slots = new CropSlot[8];

    public int SlotCount => slots.Length;

    private void Awake()
    {
        // 빈 슬롯 초기화
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new CropSlot();
        }
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
        if (slot.crop == null) return;

        // 물 요구하는 식물인데 아직 물 안 줬으면 멈추거나 말라죽게 처리
        if (slot.crop.NeedsWaterEachStage && !slot.watered)
            return;

        slot.stageTimer += dt;
        var stage = slot.crop.stages[slot.stageIndex];
        if (slot.stageTimer >= stage.duration)
        {
            slot.stageTimer = 0f;
            slot.watered = false;
            slot.stageIndex++;

            if (slot.stageIndex >= slot.crop.stages.Length - 1)
            {
                slot.state = CropSlotState.Mature;
            }

            UpdateSlotVisual(index);
        }
    }

    private void UpdateSlotVisual(int index)
    {
        var slot = slots[index];
        var point = slotPoints[index];

        // 기존 프리팹 정리
        if (slot.visual != null)
        {
            Destroy(slot.visual.gameObject);
            slot.visual = null;
        }

        if (slot.state == CropSlotState.Empty || slot.crop == null)
            return;

        GameObject prefab = null;

        if (slot.state == CropSlotState.Growing)
            prefab = slot.crop.stages[slot.stageIndex].prefab;
        else if (slot.state == CropSlotState.Mature)
            prefab = slot.crop.stages[slot.crop.stages.Length - 1].prefab;

        if (prefab != null)
        {
            var inst = Instantiate(prefab, point.position, point.rotation, point);
            slot.visual = inst.GetComponent<CropVisual>();
        }
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

    public bool CanPlant(int index, CropData crop)
    {
        if (index < 0 || index >= slots.Length) return false;
        var slot = slots[index];
        return slot.state == CropSlotState.Empty && crop != null;
    }

    public void Plant(int index, CropData crop)
    {
        var slot = slots[index];
        slot.state = CropSlotState.Growing;
        slot.crop = crop;
        slot.stageIndex = 0;
        slot.stageTimer = 0;
        slot.watered = false;

        UpdateSlotVisual(index);
    }

    public bool CanWater(int index)
    {
        var slot = slots[index];
        return slot.state == CropSlotState.Growing && !slot.watered;
    }

    public void Water(int index)
    {
        slots[index].watered = true;
        // 물 준 이펙트나 사운드 여기서
    }

    public bool CanHarvest(int index)
    {
        var slot = slots[index];
        return slot.state == CropSlotState.Mature;
    }

    public CropData Harvest(int index)
    {
        var slot = slots[index];
        if (slot.state != CropSlotState.Mature) return null;

        var crop = slot.crop;

        slot.state = CropSlotState.Empty;
        slot.crop = null;
        slot.stageIndex = 0;
        slot.stageTimer = 0;
        slot.watered = false;

        UpdateSlotVisual(index);

        return crop;
    }
}
