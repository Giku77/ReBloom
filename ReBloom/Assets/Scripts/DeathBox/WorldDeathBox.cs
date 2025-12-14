using UnityEngine;

public class WorldDeathBox : WorldItemContainerBase
{
    [Header("DeathBox References")]
    [SerializeField] private DeathBoxData deathBoxDataRef;
    private DeathBoxData deathBoxData;

    // 부모가 요구하는 Container 속성 구현
    protected override IItemContainer Container => deathBoxData;

    // 시체박스는 길게 눌러야 회수
    public override float HoldTime => 1f;

    protected override void Awake()
    {
        base.Awake();

        // 런타임 인스턴스 생성
        if (deathBoxDataRef != null)
        {
            deathBoxData = Instantiate(deathBoxDataRef);
        }

        highlight.promptFormat = "아이템 회수 [E]";
    }

    /// <summary>
    /// 외부에서 초기화 (PlayerDeathHandler에서 호출)
    /// </summary>
    public void Initialize(DeathBoxData data, IItemContainer currentPlayerInventory)
    {
        deathBoxData = data;

        // playerInventory 설정
        if (currentPlayerInventory is InventoryItemData inventory)
        {
            playerInventory = inventory;
        }

        // 플레이어 찾기
        FindPlayer();
    }

    // 시체박스 전용: 회수 후 오브젝트 제거
    protected override void OnTransferComplete()
    {
        base.OnTransferComplete();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 런타임 SO 정리
        if (deathBoxData != null && deathBoxData != deathBoxDataRef)
        {
            Destroy(deathBoxData);
        }
    }
}