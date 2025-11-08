using UnityEngine;

/// <summary>
/// 모든 아이템의 기본 추상 클래스
/// BG Database 데이터를 래핑하는 인터페이스 역할
/// </summary>
public abstract class ItemBase : ScriptableObject
{
    [Header("기본 정보")]
    public int itemID;              // 아이템 고유 ID
    public string itemName;         // 아이템 이름
    public string description;      // 설명

    [Header("분류")]
    public InventorySlotType slotType;  // 인벤토리 슬롯 타입
    public int tier;                    // 티어 (1~3)

    [Header("인벤토리 설정")]
    public int maxCount = 1;       // 최대 중첩 개수
    public bool canUseable = false;   // 사용 가능
    public bool canQuickSlot = false;   // 퀵슬롯 등록 가능
    public bool canDiscard = true;      // 버리기 가능
    public bool canStorage = true;      // 창고 저장 가능

    [Header("비주얼")]
    public Sprite icon;                 // UI 아이콘
    public string worldPrefabAddress;   // 월드 드롭 프리팹 주소

    /// <summary>
    /// 아이템 사용 (각 타입별로 구현)
    /// </summary>
    public abstract bool Apply(PlayerController player);

    /// <summary>
    /// 아이템 획득 시
    /// </summary>
    public virtual void OnAcquire() { }

    /// <summary>
    /// 아이템 제거 시
    /// </summary>
    public virtual void OnRemove() { }

    /// <summary>
    /// 유니티 이벤트
    /// </summary>
}