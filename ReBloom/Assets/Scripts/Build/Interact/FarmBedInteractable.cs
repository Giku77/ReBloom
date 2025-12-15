using UnityEngine;
using UnityEngine.InputSystem;

public class FarmBedInteractable : BuildingInteractableBase
{
    [SerializeField] private FarmBed farmBed;
    [SerializeField] private LayerMask farmSlotMask;

    private FarmUI farmUI;

    private int lastHighlightedIndex = -1;

    private void Start()
    {
        farmUI = UIManager.Instance.GetUI<FarmUI>(UIType.Farm);
    }

    public override void Interact(PlayerController player)
    {
        if (farmBed == null || player == null) return;

        int focus = GetTargetSlotIndex(); // 바라본 칸이 있으면 그 칸으로 포커스
        OpenSeedUI(player, focus);
    }


    private void Update()
    {
        if (farmBed == null) return;
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput) return; // UI 열리면 하이라이트/레이캐스트 멈추기

        int idx = GetTargetSlotIndex();
        if (idx == lastHighlightedIndex) return;

        if (lastHighlightedIndex != -1)
            farmBed.SetSlotHighlighted(lastHighlightedIndex, false);

        if (idx != -1)
            farmBed.SetSlotHighlighted(idx, true);

        lastHighlightedIndex = idx;
    }

    private int GetTargetSlotIndex()
    {
        var cam = Camera.main;
        if (cam == null) return -1;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out var hit, 10f, farmSlotMask))
            return -1;

        var slotIndexComp = hit.collider.GetComponent<FarmSlotIndex>()
                        ?? hit.collider.GetComponentInParent<FarmSlotIndex>();

        return slotIndexComp != null ? slotIndexComp.index : -1;
    }

    private void OpenSeedUI(PlayerController player, int slotIndex)
    {
        if (farmUI == null)
        {
            ToastMessageUI.Instance?.Show("FarmUI가 연결되지 않았습니다.");
            return;
        }

        // - player.InventoryData에서 seed만 필터링
        // - farmDB로 seedItemId -> cropRow 매핑
        // - SeedSlotUI들 생성/갱신
        farmUI.Open(farmBed, player, slotIndex);
    }

    private bool TryWater(PlayerController player, int slotIndex)
    {
        // TODO: 물뿌리개/물통 체크는 너 시스템에 맞게 추가
        if (!farmBed.CanWater(slotIndex)) return false;

        farmBed.Water(slotIndex);
        ToastMessageUI.Instance?.Show("물을 주었습니다.");
        return true;
    }

    private bool TryHarvest(PlayerController player, int slotIndex)
    {
        if (!farmBed.CanHarvest(slotIndex)) return false;

        if (!farmBed.TryHarvest(slotIndex, out var row)) return false;

        // 드랍 지급 (Item1/2 보장, Item3 확률)
        foreach (var d in row.drops)
        {
            if (d.rate < 1f && Random.value > d.rate) continue;
            player.Inventory.AddItem(d.itemId, d.count);
        }

        ToastMessageUI.Instance?.Show($"{row.cropName} 수확 완료!");
        return true;
    }
}
