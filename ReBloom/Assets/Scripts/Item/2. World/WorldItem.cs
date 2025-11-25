using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    private ItemBase itemData;
    private int quantity = 1;

    [Header("Interaction")]
    [SerializeField] private float pickupRange = 2f; //상호작용 가능한 범위
    [SerializeField] private LayerMask playerLayer;

    [Header("Inventory Reference")]
    [SerializeField] private InventoryItemData inventoryData;

    private PooledItem pooledItem;
    public float HoldTime => 0f;

    private void Awake()
    {
        pooledItem = GetComponent<PooledItem>();
    }
    public void Initialize(ItemBase item)
    {
        itemData = item;
        quantity = 1;
    }

    /// <summary>
    /// 아이템 수량 설정 (스택 아이템용)
    /// </summary>
    public void SetQuantity(int amount)
    {
        //풀에 돌아갈때 1로 초기화 필요
        quantity = Mathf.Max(1, amount);
    }

    private void Update()
    {
        // 플레이어가 가까우면 줍기 가능
        //CheckPickup();
    }

    private void CheckPickup()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRange, playerLayer);

        if (colliders.Length > 0 && Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickupItem();
        }
    }

    private bool PickupItem()
    {
        //var inv = (GameInventory)QuestManager.I.Inventory;
        //inv?.AddItem(itemData.itemID, quantity);
        if (inventoryData == null || itemData == null)
        {
            return false;
        }

        bool success = inventoryData.AddItem(itemData.itemID, quantity);

        if (!success) return false;

        Debug.Log($"{itemData.itemName} 획득!");

        // 풀로 반환
        if (pooledItem != null)
        {
            pooledItem.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
        return true;
    }

    public void Interact(PlayerController player)
    {
        PickupItem();
    }

    private void OnEnable()
    {
        // 일정 시간 후 자동 회수
        if (pooledItem != null)
        {
            pooledItem.ReturnToPoolAfterDelay(600f); // 10분
        }
    }

    public bool CanInteract()
    {
        return true;
    }
    
    public void ResetItem()
    {
        itemData = null;
        quantity = 1;
    }
}