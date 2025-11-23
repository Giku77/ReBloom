using UnityEngine;

public abstract class WorldItemContainerBase : MonoBehaviour, IInteractable
{
    [Header("Container Settings")]
    [SerializeField] protected InventoryItemData playerInventory;

    protected Transform playerTransform;
    protected InteractionHighlight highlight;

    // 자식 클래스가 구현해야 할 것
    protected abstract IItemContainer Container { get; }

    // IInteractable 구현
    public virtual float HoldTime => 0f; // 기본값: 즉시 상호작용

    protected virtual void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
        FindPlayer();
    }

    protected void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    public virtual bool CanInteract()
    {
        return Container != null && Container.HasItems;
    }

    // 기본 상호작용: 전부 회수
    public virtual void Interact(PlayerController player)
    {
        if (Container == null || playerInventory == null)
        {
            Debug.LogError($"[{GetType().Name}] 데이터가 초기화되지 않았습니다!");
            return;
        }

        TransferAllToPlayer();
    }

    // 공용 메서드: 플레이어에게 전부 이동
    protected void TransferAllToPlayer()
    {
        if (!Container.HasItems)
        {
            Debug.LogWarning($"[{GetType().Name}] 컨테이너가 비어있습니다!");
            return;
        }

        // IItemContainer 인터페이스로 통일된 방식!
        Container.TransferAllTo(playerInventory);

        Debug.Log($"[{GetType().Name}] 모든 아이템을 회수했습니다!");

        OnTransferComplete();
    }

    // 자식이 오버라이드 가능 (회수 후 동작)
    protected virtual void OnTransferComplete()
    {
        highlight?.Hide();
    }
}
