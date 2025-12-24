using UnityEngine;

public class WorldDeathBox : WorldItemContainerBase
{
    private DeathBoxData deathBoxData;

    // 부모가 요구하는 Container 속성 구현
    protected override IItemContainer Container => deathBoxData;

    // 시체박스는 길게 눌러야 회수
    public override float HoldTime => 1f;

    protected override void Awake()
    {
        base.Awake();

        deathBoxData = ScriptableObject.CreateInstance<DeathBoxData>();
    }
    /// </summary>
    public void Initialize(DeathBoxData data)
    {
        deathBoxData = data;

        // playerInventory는 항상 찾아야 함
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<GameInventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogError("[WorldDeathBox] GameInventory를 찾을 수 없습니다!");
        }

        FindPlayer();
    }

    // 시체박스 전용: 회수 후 오브젝트 제거
    protected override void OnTransferComplete()
    {
        Debug.Log($"[WorldDeathBox] BEFORE base: hasItems={deathBoxData?.HasItems} count={deathBoxData?.Items.Count}");
        base.OnTransferComplete();
        Debug.Log($"[WorldDeathBox] AFTER  base: hasItems={deathBoxData?.HasItems} count={deathBoxData?.Items.Count}");
        if (deathBoxData != null && deathBoxData.HasItems)
        {
            ToastMessageUI.Instance?.Show("인벤토리가 가득 찼습니다. 남은 아이템이 있습니다.");
            return;
        }
        Destroy(gameObject);
    }
    public override bool CanInteract()
    {
        bool canInteract = deathBoxData != null && deathBoxData.HasItems;
        Debug.Log($"[WorldDeathBox] CanInteract: {canInteract} (data: {deathBoxData != null}, hasItems: {deathBoxData?.HasItems})");
        return canInteract;
    }
    public override void Interact(PlayerController player)
    {


        Debug.Log($"[WorldDeathBox] Interact 호출됨");
        Debug.Log($"[WorldDeathBox] deathBoxData: {(deathBoxData != null ? "있음" : "NULL")}");
        Debug.Log($"[WorldDeathBox] playerInventory: {(playerInventory != null ? "있음" : "NULL")}");

        if (deathBoxData != null)
        {
            Debug.Log($"[WorldDeathBox] HasItems: {deathBoxData.HasItems}");
            Debug.Log($"[WorldDeathBox] Items.Count: {deathBoxData.Items.Count}");
        }
        highlight.promptFormat = "아이템 회수 [E]";
        base.Interact(player);
    }

    private void OnDestroy()
    {
        // 런타임 SO 정리
        if (deathBoxData != null)
        {
            Destroy(deathBoxData);
        }
    }
}