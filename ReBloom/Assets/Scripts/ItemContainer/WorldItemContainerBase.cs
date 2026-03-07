using UnityEngine;

public abstract class WorldItemContainerBase : MonoBehaviour, IInteractable
{
    [Header("Container Settings")]
    [SerializeField] protected GameInventory playerInventory;

    protected Transform playerTransform;
    protected InteractionHighlight highlight;

    protected abstract IItemContainer Container { get; }

    public virtual float HoldTime => 0f;

    protected virtual void Awake()
    {
        highlight = GetComponent<InteractionHighlight>();
        FindPlayer();
    }

    protected virtual void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
    }

    protected virtual void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
    }

    protected void FindPlayer()
    {
        if (playerInventory != null && playerTransform != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            BindLocalPlayer(player);
        }
    }

    protected void BindLocalPlayer(GameObject player)
    {
        if (player == null)
            return;

        playerTransform = player.transform;

        if (playerInventory == null)
            playerInventory = player.GetComponent<GameInventory>();
    }

    public virtual bool CanInteract()
    {
        return Container != null && Container.HasItems;
    }

    public virtual void Interact(PlayerController player)
    {
        if (Container == null || playerInventory == null)
        {
            Debug.LogError($"[{GetType().Name}] 데이터가 초기화되지 않았습니다!");
            return;
        }

        SoundManager.I?.PlayOpenBox();
        TransferAllToPlayer();
    }

    protected void TransferAllToPlayer()
    {
        if (!Container.HasItems)
            return;

        bool success = ItemTransferUtility.TransferAll(Container, playerInventory.Container);

        if (success)
            OnTransferComplete();
    }

    protected virtual void OnTransferComplete()
    {
        highlight?.Hide();
    }
}
