using UnityEngine;

public class FarmBedInteractable : BuildingInteractableBase
{
    [SerializeField] private FarmBed farmBed;
    //[SerializeField] private Camera playerCamera;
    [SerializeField] private BoxCollider bedCollider;
    [SerializeField] private LayerMask farmSlotMask; 
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows    = 2;

    [SerializeField] private CropData testCropData; // 테스트

    private int lastHighlightedIndex = -1;

    public override void Interact(PlayerController player)
    {
        if (farmBed == null) return;

        int slotIndex = GetTargetSlotIndex();
        if (slotIndex < 0) return;

        if (TryPlant(player, slotIndex)) return;

        if (TryWater(player, slotIndex)) return;

        if (TryHarvest(player, slotIndex)) return;
    }

    private void Update()
    {
        if (farmBed == null) return;

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


    private bool TryPlant(PlayerController player, int slotIndex)
    {
        // 예시: 플레이어 현재 선택 슬롯 아이템 가져오기
        //var item = player.Inventory.GetSelectedItem();
        //if (item == null) return false;

        //CropData crop = CropDB.I.GetCropBySeedItem(item); // 나중에 DB 사용
        CropData crop = testCropData; // 테스트용

        Debug.Log($"TryPlant: slotIndex={slotIndex}, crop={(crop != null ? crop.cropName : "null")}");

        if (crop == null) return false;
        if (!farmBed.CanPlant(slotIndex, crop)) return false;

        Debug.Log("Planting crop...");  

        farmBed.Plant(slotIndex, crop);

        ToastMessageUI.Instance?.Show($"{crop.cropName}을(를) 심었습니다.");

        // 씨앗 소모
        //player.Inventory.RemoveItem(item, 1);

        return true;
    }

    private bool TryWater(PlayerController player, int slotIndex)
    {
        // 물뿌리개 체크: 아이템 타입으로 판별
        // var item = player.Inventory.GetSelectedItem();
        // if (item == null || !item.IsWateringCan) return false;

        if (!farmBed.CanWater(slotIndex)) return false;

        farmBed.Water(slotIndex);

        ToastMessageUI.Instance?.Show("물을 주었습니다.");

        return true;
    }

    private bool TryHarvest(PlayerController player, int slotIndex)
    {
        if (!farmBed.CanHarvest(slotIndex)) return false;

        var crop = farmBed.Harvest(slotIndex);
        if (crop == null) return false;

        // 수확 아이템 지급
        var harvestItem = ItemDatabase.I.GetItem(crop.harvestItemId);
        player.Inventory.AddItem(crop.harvestItemId, 1); // 수량은 나중에 crop 데이터에 넣기
        ToastMessageUI.Instance?.Show($"{harvestItem.itemName}을(를) 수확했습니다.");
        return true;
    }
}
