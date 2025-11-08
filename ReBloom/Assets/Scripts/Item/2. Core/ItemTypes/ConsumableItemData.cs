using BansheeGz.BGDatabase;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 소비 아이템 (BG Database 래퍼)
/// 구글시트의 소비아이템 테이블 데이터를 Unity에서 사용
/// </summary>
public class ConsumableItemData : ItemBase
{
    // BG Database 원본 Entity
    private BGEntity entity;

    // 필드 참조 캐싱
    private BGField<int> ConsumeItem_ID;
    private BGField<string> ConsumeItem_Name;
    private BGField<int> Inventory_N;
    private BGField<int> Tier;
    private BGField<int> M_Cat;
    private BGField<int> S_Cat;
    private BGField<int> MaxCount;
    private BGField<int> Useable;
    private BGField<int> Quickable;
    private BGField<int> Discardable;
    private BGField<int> Storageable;
    private BGField<float> Pollution;
    private BGField<float> Thirst;
    private BGField<float> HP;
    private BGField<float> fieldHP;
    private BGField<float> Temp;
    private BGField<int> Range;
    private BGField<float> Duration;
    private BGField<string> ImgPath;
    private BGField<string> Description;

    /// <summary>
    /// BG Database Entity로 초기화
    /// </summary>
    public void Initialize(BGEntity entity)
    {
        this.entity = entity;

        var meta = entity.Meta;

        ConsumeItem_ID = meta.GetField<int>("ConsumeItem_ID");
        ConsumeItem_Name = meta.GetField<string>("ConsumeItem_Name");
        Inventory_N = meta.GetField<int>("Inventory_N");
        Tier = meta.GetField<int>("Tier");
        M_Cat = meta.GetField<int>("M_Cat");
        S_Cat = meta.GetField<int>("S_Cat");
        MaxCount = meta.GetField<int>("MaxCount");
        Quickable = meta.GetField<int>("Quickable");
        Discardable = meta.GetField<int>("Discardable");
        Storageable = meta.GetField<int>("Storageable");
        Pollution = meta.GetField<float>("Pollution");
        Thirst = meta.GetField<float>("Thirst");
        HP = meta.GetField<float>("Hunger");
        fieldHP = meta.GetField<float>("HP");
        Temp = meta.GetField<float>("Temp");
        Range = meta.GetField<int>("Range");
        Duration = meta.GetField<float>("Duration");
        ImgPath = meta.GetField<string>("ImgPath");
        Description = meta.GetField<string>("Description");

        itemID = ConsumeItem_ID[entity];
        itemName = ConsumeItem_Name[entity];
        slotType = (InventorySlotType)Inventory_N[entity];
        tier = Tier[entity];
        maxCount = MaxCount[entity];
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canDiscard = Convert.ToBoolean(Discardable[entity]);
        canStorage = Convert.ToBoolean(Storageable[entity]);
        description = Description[entity];

        // 아이콘은 Addressable로 비동기 로드
        // LoadIconAsync();
    }

    /// <summary>
    /// 아이템 사용 (소비)
    /// </summary>
    public override bool Apply(PlayerController player)
    {
        if (player == null) return false;

        // 실시간으로 BG Database에서 최신 수치 읽기
        // (구글시트 수정 후 BG Database 동기화하면 자동 반영됨)
        float pollution = Pollution[entity];
        float thirst = Thirst[entity];
        float hunger = HP[entity];
        float hp = fieldHP[entity];
        float temp = Temp[entity];

        // TODO: 플레이어 스탯 적용

        // 특수 효과 (재밍 아이템)
        int mainCat = M_Cat[entity];
        if (mainCat == (int)ConsumableCategory.Jamming)
        {
            float range = Range[entity];
            float duration = Duration[entity];
            //TODO: 재밍 펄스 생성
        }

        // VFX/SFX 재생
        // PlayUseEffect(player.transform.position);

        Debug.Log($"[아이템 사용] {itemName} - HP:{hp}, 오염도:{pollution}, 갈증:{thirst}");
        return true;
    }

    /// <summary>
    /// Addressable로 아이콘 비동기 로드
    /// </summary>
    private async void LoadIconAsync()
    {
        string path = ImgPath[entity];
        if (string.IsNullOrEmpty(path)) return;

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(path);
        await handle.Task;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            icon = handle.Result;
        }
        else
        {
            Debug.LogWarning($"[ItemData] 아이콘 로드 실패: {path}");
        }
    }

    /// <summary>
    /// 사용 효과 재생 (VFX/SFX)
    /// </summary>
    private void PlayUseEffect(Vector3 position)
    {
        // TODO: TA 작업 - VFX/SFX 시스템과 연동
        // VFXManager.Instance.Play("ItemUse_" + itemName, position);
        // SFXManager.Instance.Play("ItemUse_Sound");
    }
}