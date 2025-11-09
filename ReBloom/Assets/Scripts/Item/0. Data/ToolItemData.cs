using BansheeGz.BGDatabase;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 도구 아이템 (BG Database 래퍼)
/// 구글시트의 도구 아이템 테이블 데이터를 Unity에서 사용
/// 삽, 곡괭이, 가방 등
/// </summary>
public class ToolItemData : ItemBase
{
    private BGEntity entity;

    private BGField<int> Tool_ID;
    private BGField<string> Tool_Name;
    private BGField<int> Inventory_N;
    private BGField<int> Category;
    private BGField<int> Tier;
    private BGField<int> Useable;
    private BGField<int> Quickable;
    private BGField<int> MaxCount;
    private BGField<int> Discardable;
    private BGField<int> Storageable;
    private BGField<int> Using;
    private BGField<float> Perform;
    private BGField<string> Img_Path;
    private BGField<string> Description;

    #region 도구 전용 속성
    /// <summary>
    /// 도구 카테고리 (1=삽, 2=곡괭이, 3=가방)
    /// </summary>
    public ToolCategory toolCategory { get; private set; }

    /// <summary>
    /// 사용 중 여부 (0=미사용, 1=사용중)
    /// </summary>
    public bool isUsing { get; private set; }

    /// <summary>
    /// 성능 수치 (작업 시간 단축 등)
    /// </summary>
    public float performance { get; private set; }
    #endregion

    /// <summary>
    /// BG Database Entity로 초기화
    /// </summary>
    public void Initialize(BGEntity entity)
    {
        this.entity = entity;

        var meta = entity.Meta;

        // 필드 참조 가져오기
        Tool_ID = meta.GetField<int>("Tool_ID");
        Tool_Name = meta.GetField<string>("Tool_Name");
        Inventory_N = meta.GetField<int>("Inventory_N");
        Category = meta.GetField<int>("Category");
        Tier = meta.GetField<int>("Tier");
        Useable = meta.GetField<int>("Useable");
        Quickable = meta.GetField<int>("Quickable");
        MaxCount = meta.GetField<int>("MaxCount");
        Discardable = meta.GetField<int>("Discardable");
        Storageable = meta.GetField<int>("Storageable");
        Using = meta.GetField<int>("Using");
        Perform = meta.GetField<float>("Perform");
        Img_Path = meta.GetField<string>("Img_Path");
        Description = meta.GetField<string>("Description");

        // 기본 정보
        itemID = Tool_ID[entity];
        itemName = Tool_Name[entity];
        slotType = (InventorySlotType)Inventory_N[entity];
        tier = Tier[entity];
        maxCount = MaxCount[entity];
        canUseable = Convert.ToBoolean(Useable[entity]);
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canDiscard = Convert.ToBoolean(Discardable[entity]);
        canStorage = Convert.ToBoolean(Storageable[entity]);
        description = Description[entity];

        // 도구 전용 속성
        toolCategory = (ToolCategory)Category[entity];
        isUsing = Convert.ToBoolean(Using[entity]);
        performance = Perform[entity];

        // 아이콘은 Addressable로 비동기 로드
        LoadIconAsync();
    }

    /// <summary>
    /// 도구 사용 (장착)
    /// </summary>
    public override bool Apply(PlayerController player)
    {
        if (player == null) return false;

        // 실시간으로 BG Database에서 최신 수치 읽기
        float currentPerform = Perform[entity];
        ToolCategory category = (ToolCategory)Category[entity];

        switch (category)
        {
            case ToolCategory.Shovel:
                // 삽: 땅 파기 속도 증가
                Debug.Log($"[도구 장착] {itemName} - 파기 시간 {currentPerform}초");
                // TODO: 플레이어에게 파기 속도 버프 적용
                break;

            case ToolCategory.Pickaxe:
                // 곡괭이: 채광 속도 증가
                Debug.Log($"[도구 장착] {itemName} - 채광 시간 {currentPerform}초");
                // TODO: 플레이어에게 채광 속도 버프 적용
                break;

            case ToolCategory.Bag:
                // 가방: 인벤토리 확장
                Debug.Log($"[도구 장착] {itemName} - 인벤토리 슬롯 증가");
                // TODO: 인벤토리 크기 확장
                break;
        }

        // 장착 VFX/SFX
        // PlayEquipEffect(player.transform.position);

        return true;
    }

    /// <summary>
    /// 도구 해제
    /// </summary>
    public void Unequip(PlayerController player)
    {
        if (player == null) return;

        Debug.Log($"[도구 해제] {itemName}");

        // TODO: 버프 제거, 인벤토리 복구 등
    }

    /// <summary>
    /// Addressable로 아이콘 비동기 로드
    /// </summary>
    private async void LoadIconAsync()
    {
        //string path = Img_Path[entity];
        string path = "Icon/ToolIcon"; // 임시 경로

        // 경로가 비어있으면 기본 아이콘 사용
        if (string.IsNullOrEmpty(path))
        {
            path = "Icon/ItemIcon"; // 기본 경로
        }

        try
        {
            // GameObject(Prefab)로 로드
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.GameObject>(path);
            await handle.Task;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                UnityEngine.GameObject prefab = handle.Result;

                // Image 컴포넌트에서 Sprite 추출 (루트)
                var image = prefab.GetComponent<UnityEngine.UI.Image>();
                if (image != null && image.sprite != null)
                {
                    icon = image.sprite;
                    Debug.Log($"[ToolItemData] 아이콘 로드 성공: {itemName}");
                    return;
                }

                // Image가 자식에 있는 경우
                image = prefab.GetComponentInChildren<UnityEngine.UI.Image>();
                if (image != null && image.sprite != null)
                {
                    icon = image.sprite;
                    Debug.Log($"[ToolItemData] 아이콘 로드 성공 (자식): {itemName}");
                    return;
                }

                Debug.LogWarning($"[ToolItemData] Prefab에 Image 컴포넌트가 없거나 Sprite가 없음: {path}");
            }
            else
            {
                Debug.LogWarning($"[ToolItemData] 아이콘 로드 실패: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ToolItemData] 아이콘 로드 예외: {path}\n{e.Message}");
        }
    }
    /// <summary>
    /// 장착 효과 재생 (VFX/SFX)
    /// </summary>
    private void PlayEquipEffect(Vector3 position)
    {
        // TODO: TA 작업 - VFX/SFX 시스템과 연동
        // VFXManager.Instance.Play("ToolEquip_" + toolCategory, position);
        // SFXManager.Instance.Play("Tool_Equip_Sound");
    }
}