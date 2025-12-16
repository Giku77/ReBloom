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
        base.Awake();


       deathBoxData = ScriptableObject.CreateInstance<DeathBoxData>();
       highlight.promptFormat = "아이템 회수 [E]";
    /// </summary>
    public void Initialize(DeathBoxData data, IItemContainer currentPlayerInventory)
    {
        deathBoxData = data;

        if (currentPlayerInventory != null)
        {
            playerInventory = GameObject.FindFirstObjectByType<GameInventory>();
        }

        // 플레이어 찾기
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

    // public override void Interact(PlayerController player)
    // {
    //     if (deathBoxData != null)
    //     {
    //         Debug.Log($"[WorldDeathBox] 상호작용 - 아이템 수: {deathBoxData.Items.Count}");
    //     }
    //     //base.Interact(player);
    // }

    private void OnDestroy()
    {
        // 런타임 SO 정리
        if (deathBoxData != null)
        {
            Destroy(deathBoxData);
        }
    }
}