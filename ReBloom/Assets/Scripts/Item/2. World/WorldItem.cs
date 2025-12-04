using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    private ItemBase itemData;
    
    private bool isPersistent = false; // 영구 아이템 플래그
private int quantity = 1;

    [Header("Interaction")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Item Type")]
    [SerializeField] private bool isPlacedItem = false;

    private PooledItem pooledItem;
    public float HoldTime => 0f;

    private void Awake()
    {
        pooledItem = GetComponent<PooledItem>();
    }
    public void Initialize(ItemBase item, bool isDropped = true)
    {
        itemData = item;
        quantity = 1;
        isPlacedItem = !isDropped;// 드랍된 아이템이 아니면 배치 아이템

        // 드랍 아이템이면 타이머 시작
        if (isDropped && pooledItem != null)
        {
            pooledItem.ReturnToPoolAfterDelay(600f); // 10분
        }
    }

    /// <summary>
    /// 아이템 수량 설정 (스택 아이템용)
    /// </summary>
public void SetQuantity(int amount)
    {
        quantity = Mathf.Max(1, amount);
    }

    /// <summary>
    /// 현재 아이템 데이터 반환 (외부 참조용)
    /// </summary>
    public ItemBase GetItemData() => itemData;

    /// <summary>
    /// 현재 수량 반환
    /// </summary>
    public int GetQuantity() => quantity;
    private void Update()
    {
        // 플레이어가 가까우면 줍기 가능
        //CheckPickup();
    }

    //private void CheckPickup()
    //{
    //    Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRange, playerLayer);

    //    if (colliders.Length > 0 && Keyboard.current.eKey.wasPressedThisFrame)
    //    {
    //        PickupItem();
    //    }
    //}

    private bool PickupItem(PlayerController player)
    {
        var inventoryData = player.Inventory;
        if (inventoryData == null || itemData == null)
        {
            return false;
        }

 // 부분 습득 지원
        int addedCount = inventoryData.AddItem(itemData.itemID, quantity);

        if (addedCount <= 0)
        {
            // 하나도 못 줍음
            Debug.LogWarning($"인벤토리 가득 참! {itemData.itemName} 습득 실패");
            return false;
        }
        if (addedCount < quantity)
        {
            // 일부만 습득됨 - 남은 수량 유지
            quantity -= addedCount;
            Debug.Log($"{itemData.itemName} {addedCount}/{quantity + addedCount}개 획득! (남은 수량: {quantity})");

            // 맵 배치 아이템이었다면 드랍 아이템으로 전환
            if (isPlacedItem)
            {
                isPlacedItem = false;
                if (pooledItem != null)
                {
                    pooledItem.ReturnToPoolAfterDelay(600f); // 10분 타이머 시작
                }
            }
            return true;
        }
        // 전부 습득 성공
        Debug.Log($"{itemData.itemName} {addedCount}개 획득!");

        // 오브젝트 제거 또는 풀 반환
        if (pooledItem != null)
        {
            pooledItem.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }

        TutorialEventBus.RaiseAction((int)TutorialActionId.PickupItem);
        return true;
    }

    public void Interact(PlayerController player)
    {
        PickupItem(player);
    }

    public bool CanInteract()
    {
        return true;
    }
    
public void ResetItem()
    {
        itemData = null;
        quantity = 1;
        isPlacedItem = false;
    }
}