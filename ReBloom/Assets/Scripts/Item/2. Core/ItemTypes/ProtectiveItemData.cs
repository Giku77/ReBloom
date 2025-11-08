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
    private BGField<float> Extra_HP;
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
        Extra_HP = meta.GetField<float>("Extra_HP");
        Defense = meta.GetField<int>("Defense");
        Insulation = meta.GetField<float>("Insulation");

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
}