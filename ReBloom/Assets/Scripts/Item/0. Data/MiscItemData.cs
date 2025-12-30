using BansheeGz.BGDatabase;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

/// <summary>
/// 기타 아이템 (BG Database 래퍼)
/// 구글시트의 기타 아이템 테이블 데이터를 Unity에서 사용
/// 종자, 자원, 재료, 특수 아이템 등
/// </summary>
public class MiscItemData : ItemBase
{
    private BGEntity entity;

    private BGField<int> Item_ID;
    private BGField<string> Item_Name;
    private BGField<int> Inventory_N;
    private BGField<int> Major_Cat;
    private BGField<int> Minor_Cat;
    private BGField<int> Variation;
    private BGField<int> Quickable;
    private BGField<int> MaxCount;
    private BGField<int> Discardable;
    private BGField<int> Storageable;
    private BGField<string> Imgpath;
    private BGField<string> Description;
    private BGField<string> Addressable_Key;

    #region 기타 아이템 전용 속성
    public MiscItemCategory miscCategory { get; private set; }
    public int variation { get; private set; }
    #endregion

    public void Initialize(BGEntity entity)
    {
        this.entity = entity;
        var meta = entity.Meta;

        Item_ID = meta.GetField<int>("Item_ID");
        Item_Name = meta.GetField<string>("Item_Name");
        Inventory_N = meta.GetField<int>("Inventory_N");
        Major_Cat = meta.GetField<int>("Major_Cat");
        Minor_Cat = meta.GetField<int>("Minor_Cat");
        Variation = meta.GetField<int>("Variation");
        Quickable = meta.GetField<int>("Quickable");
        MaxCount = meta.GetField<int>("MaxCount");
        Discardable = meta.GetField<int>("Discardable");
        Storageable = meta.GetField<int>("Storageable");
        Imgpath = meta.GetField<string>("Img_Path");
        Description = meta.GetField<string>("Description");
        Addressable_Key = meta.GetField<string>("Addressable_Key");

        itemID = Item_ID[entity];
        itemName = Item_Name[entity];
        slotType = (InventorySlotType)Inventory_N[entity];
        miscCategory = (MiscItemCategory)Major_Cat[entity];
        variation = Variation[entity];
        maxCount = MaxCount[entity];
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canDiscard = Convert.ToBoolean(Discardable[entity]);
        canStorage = Convert.ToBoolean(Storageable[entity]);
        description = Description[entity].ToString();
        canUseable = false;
        worldPrefabAddress = Addressable_Key[entity];

        iconAddress = "Assets/Rebloom_Arts/Icon/Item/" + worldPrefabAddress + "_icon.png";

        LoadIconAsync();
        LoadPrefabAsync();
    }

    public override bool Apply(PlayerController player)
    {
        if (player == null) return false;

        if (miscCategory == MiscItemCategory.ImportantItem)
        {
            return UseSpecialItem(player);
        }

        Debug.LogWarning($"[기타 아이템] {itemName}은(는) 사용할 수 없는 아이템입니다.");
        return false;
    }

    private bool UseSpecialItem(PlayerController player)
    {
        string name = Item_Name[entity];
        Debug.Log($"[특수 아이템 사용] {name}");

        switch (itemID)
        {
            case 2003001:
                Debug.Log("건축물을 철거할 수 있습니다.");
                break;
            case 2003002:
                Debug.Log("잠긴 구역에 접근할 수 있습니다.");
                break;
            default:
                Debug.LogWarning($"미구현 특수 아이템: {itemID}");
                return false;
        }

        return true;
    }

    public bool PlantSeed(Vector3 position)
    {
        if (miscCategory != MiscItemCategory.Seed && miscCategory != MiscItemCategory.UnidentifiedSeed)
        {
            Debug.LogWarning($"[기타 아이템] {itemName}은(는) 종자가 아닙니다.");
            return false;
        }

        Debug.Log($"[종자 심기] {itemName} - 변종 레벨: {variation}");
        return true;
    }

    public bool IsCraftingMaterial()
    {
        return miscCategory == MiscItemCategory.NaturalMaterial ||
               miscCategory == MiscItemCategory.ProcessedMaterial;
    }

    /// <summary>
    /// Addressable Prefab(GameObject)에서 Image 컴포넌트의 Sprite 추출
    /// </summary>
    private async void LoadIconAsync()
    {
        //string path = ImgPath[entity];  // 임시 경로
        string path = iconAddress;

        // 경로가 비어있으면 기본 아이콘 사용
        if (string.IsNullOrEmpty(path) || worldPrefabAddress == null)
        {
            path = "Icon/ItemIcon"; // 기본 경로
        }

        try
        {
            // GameObject(Prefab)로 로드
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Sprite>(path);
            await handle.Task;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                UnityEngine.Sprite sprite = handle.Result;

                // Image 컴포넌트에서 Sprite 추출 (루트)

                if (sprite != null)
                {
                    icon = sprite;
                    return;
                }

                Debug.LogWarning($"[ConsumableItemData] Prefab에 Image 컴포넌트가 없거나 Sprite가 없음: {path}");
            }
            else
            {
                Debug.LogWarning($"[ConsumableItemData] 아이콘 로드 실패: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ConsumableItemData] 아이콘 로드 예외: {path}\n{e.Message}");
        }
    }

    /// <summary>
    /// Addressable로 아이콘 비동기 로드
    /// </summary>
    private async void LoadPrefabAsync()
    {
        //string path = Img_Path[entity];
        string path = Addressable_Key[entity];

        // 경로가 비어있으면 기본 아이콘 사용
        if (string.IsNullOrEmpty(path))
        {
            path = "Item/Item00"; // 기본 경로
        }

        try
        {
            // GameObject(Prefab)로 로드
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.GameObject>(path);
            await handle.Task;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                itemPrefab = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[ToolItemData] prefab 로드 실패: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ToolItemData] prefab 로드 예외: {path}\n{e.Message}");
        }
    }
}