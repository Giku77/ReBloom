using BansheeGz.BGDatabase;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEquipManager : MonoBehaviour
{
    private PlayerEquipData player;

    [SerializeField] private int defaultClothID = 4301002;
    [SerializeField] private int defaultShoesID = 4302002;


    [SerializeField] private InventoryItemData inventoryItemData;

    private void Awake()
    {
        player = GetComponent<PlayerEquipData>();
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {

        }
    }

    public void Apply(ProtectiveItemData item)
    {
        if (item == null)
        {
            Debug.Log("잘못 된 보호구 아이템입니다.");
            return;
        }

        inventoryItemData.RemoveItem(item.itemID, 1);

        switch (item.gearType)
        {
            case ProtectiveGearType.Clothing:
                if (player.currentClothEquip != null)
                    UnEquip(ProtectiveGearType.Clothing);

                player.currentClothEquip = item;
                break;

            case ProtectiveGearType.Shoes:
                if (player.currentShoesEquip != null)
                    UnEquip(ProtectiveGearType.Shoes);

                player.currentShoesEquip = item;
                break;
            case ProtectiveGearType.None:
                Debug.Log("장착 불가능한 보호구 타입입니다.");
                break;
            default:
                Debug.Log("잘못 된 보호구 아이템입니다.");
                return;
        }


        Debug.Log($"[EquipManager] 장착 완료: {item.itemName} (오염 저항: {item.GetPollutionResist()}%)");
    }

public void EquipItem(int itemId)
    {
        // ItemDatabase에서 아이템 데이터 가져오기
        ItemBase itemBase = ItemDatabase.I.GetItem(itemId);
        if (itemBase == null)
        {
            Debug.LogError($"[PlayerEquipManager] 아이템 ID {itemId}를 찾을 수 없습니다.");
            return;
        }
        
        // ProtectiveItemData로 변환
        ProtectiveItemData itemData = itemBase as ProtectiveItemData;
        if (itemData == null)
        {
            Debug.LogError($"[PlayerEquipManager] 아이템 ID {itemId}는 보호구 아이템이 아닙니다.");
            return;
        }
        
        Apply(itemData);
    }


    public void UnEquip(ProtectiveGearType gearType)
    {
        switch (gearType)
        {
            case ProtectiveGearType.Clothing:
                if (!player.currentClothEquip)
                    return;

                inventoryItemData.AddItem(player.currentClothEquip.itemID, 1);
                player.currentClothEquip = null;
                break;

            case ProtectiveGearType.Shoes:
                if (!player.currentShoesEquip)
                    return;

                inventoryItemData.AddItem(player.currentShoesEquip.itemID, 1);
                player.currentShoesEquip = null;
                break;
            case ProtectiveGearType.None:
                Debug.Log("잘못 된 보호구 타입입니다.");
                break;
        }

        Debug.Log($"아이템 해제 완료");
    }

        public float GetTotalPollutionResist()
    {
        float resist = 0f;
        float clothResist = 0f;
        float shoesResist = 0f;
        if (player.currentClothEquip is ProtectiveItemData cloth)
        {
            clothResist = cloth.GetPollutionResist();
            resist += clothResist;
        }
        if (player.currentShoesEquip is ProtectiveItemData shoes)
        {
            shoesResist = shoes.GetPollutionResist();
            resist += shoesResist;
        }
        
        float finalResist = Mathf.Clamp01(resist);
        
        //임시 장착 확인용
        //if (Time.frameCount % 60 == 0 && resist > 0)
        //{
        //    Debug.Log($"[EquipManager] 옥: {clothResist}, 신발: {shoesResist}, 합계: {resist} → 최종 저항: {finalResist:F2}");
        //}
        return finalResist;
    }
}
