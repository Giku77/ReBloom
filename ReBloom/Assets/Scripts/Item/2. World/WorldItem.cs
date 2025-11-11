using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class WorldItem : MonoBehaviour, IInteractable
{
    private ItemBase itemData;

    [Header("Interaction")]
    [SerializeField] private float pickupRange = 2f; //상호작용 가능한 범위
    [SerializeField] private LayerMask playerLayer;

    public void Initialize(ItemBase item)
    {
        itemData = item;
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

    private void PickupItem()
    {
        // TODO: 인벤토리에 추가


        Debug.Log($"{itemData.itemName} 획득!");

        // 월드에서 제거
        Destroy(gameObject);
    }

    public void OnInteract()
    {
        PickupItem();
    }
}