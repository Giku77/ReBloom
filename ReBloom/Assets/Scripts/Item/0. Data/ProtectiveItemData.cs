using BansheeGz.BGDatabase;
using System;
using UnityEngine;

/// <summary>
/// 보호구 아이템 (BG Database 래퍼)
/// </summary>
public class ProtectiveItemData : ItemBase
{
    private BGEntity entity;

    private BGField<int> Equip_ID;
    private BGField<string> Equip_Name;
    private BGField<int> Inventory_N;
    private BGField<int> Category;
    private BGField<int> Tier;
    private BGField<int> Useable;
    private BGField<int> Quickable;

    private BGField<int> MaxCount;
    private BGField<int> Discardable;
    private BGField<int> Storageable;
    private BGField<float> Pollution_Resist;
    private BGField<float> Height_Resist;
    private BGField<int> Extra_HP;
    private BGField<int> Defense;
    private BGField<float> Insulation;
    private BGField<String> Description;

    public ProtectiveGearType gearType;

    public float currentPopullation { get; private set; }

    public void Initialize(BGEntity entity)
    {
        this.entity = entity;
        var meta = entity.Meta;

        Equip_ID = meta.GetField<int>("Equip_ID");
        Equip_Name = meta.GetField<string>("Equip_Name");
        Inventory_N = entity.Meta.GetField<int>("Inventory_N");
        Category = meta.GetField<int>("Category");
        Tier = meta.GetField<int>("Tier");
        Useable = meta.GetField<int>("Useable");
        Quickable = meta.GetField<int>("Quickable");
        MaxCount = meta.GetField<int>("MaxCount");
        Discardable = meta.GetField<int>("Discardable");
        Storageable = meta.GetField<int>("Storageable");
        Pollution_Resist = meta.GetField<float>("Pollution_Resist");
        Height_Resist = meta.GetField<float>("Height_Resist");
        Extra_HP = meta.GetField<int>("Extra_HP");
        Defense = meta.GetField<int>("Defense");
        Insulation = meta.GetField<float>("Insulation");
        Description = meta.GetField<string>("Description");

        itemID = Equip_ID[entity];
        itemName = Equip_Name[entity];
        slotType = (InventorySlotType)Inventory_N[entity];
        tier = Tier[entity];
        maxCount = MaxCount[entity];
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canDiscard = Convert.ToBoolean(Discardable[entity]);
        canStorage = Convert.ToBoolean(Storageable[entity]);
        description = Description[entity];

        gearType = (ProtectiveGearType)Category[entity];

        LoadIconAsync();
    }

    /// <summary>
    /// 장비 장착
    /// </summary>
    public override bool Apply(PlayerController player)
    {
        // BG Database에서 최신 수치 읽기
        float pollutionResist = Pollution_Resist[entity];
        float temp = Extra_HP[entity];
        float height_resist = Defense[entity];

        // TODO: player 스탯 적용

        Debug.Log($"[장비 장착] {itemName} - 오염방어:{pollutionResist}%, 체온보너스:{temp}");

        return true;
    }

    /// <summary>
    /// 장비 해제
    /// </summary>
    public void Unequip(PlayerController player)
    {
        float pollutionResist = Pollution_Resist[entity];

        // TODO: player 스탯 remove 
    }

    /// <summary>
    /// 오염도 증가
    /// </summary>
    public void DecreasePopullation(float amount)
    {
        currentPopullation -= amount;

        if (currentPopullation <= 0)
        {
            currentPopullation = 0;
            Debug.Log($"[장비 오염] {itemName}의 오염도가 최대가 되었습니다!");
            // TODO: 장비 파괴 처리
        }
    }

    /// <summary>
    /// Addressable로 아이콘 비동기 로드
    /// </summary>
    private async void LoadIconAsync()
    {
        //string path = ImgPath[entity];
        string path = "Icon/EquipIcon"; // 임시 경로

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
                    return;
                }

                // Image가 자식에 있는 경우
                image = prefab.GetComponentInChildren<UnityEngine.UI.Image>();
                if (image != null && image.sprite != null)
                {
                    icon = image.sprite;
                    return;
                }

                Debug.LogWarning($"[ProtectiveItemData] Prefab에 Image 컴포넌트가 없거나 Sprite가 없음: {path}");
            }
            else
            {
                Debug.LogWarning($"[ProtectiveItemData] 아이콘 로드 실패: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ProtectiveItemData] 아이콘 로드 예외: {path}\n{e.Message}");
        }
    }
}