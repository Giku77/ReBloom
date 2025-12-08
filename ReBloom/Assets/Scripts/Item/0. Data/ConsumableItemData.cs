using BansheeGz.BGDatabase;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 소비 아이템 (BG Database 래퍼)
/// 구글시트의 소비아이템 테이블 데이터를 Unity에서 사용
/// </summary>
public class ConsumableItemData : ItemBase
{
    private BGEntity entity;

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
    private BGField<float> Hunger;
    private BGField<float> HP;
    private BGField<float> Temp;
    private BGField<int> Range;
    private BGField<float> Duration;
    private BGField<string> ImgPath;
    private BGField<string> Description;
    private BGField<string> Addressable_Key;

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
        Useable = meta.GetField<int>("Useable");
        Quickable = meta.GetField<int>("Quickable");
        Discardable = meta.GetField<int>("Discardable");
        Storageable = meta.GetField<int>("Storageable");
        Pollution = meta.GetField<float>("Pollution");
        Thirst = meta.GetField<float>("Thirst");
        Hunger = meta.GetField<float>("Hunger");
        HP = meta.GetField<float>("HP");
        Temp = meta.GetField<float>("Temp");
        Range = meta.GetField<int>("Range");
        Duration = meta.GetField<float>("Duration");
        ImgPath = meta.GetField<string>("ImgPath");
        Description = meta.GetField<string>("Description");
        Addressable_Key = meta.GetField<string>("Addressable_Key");

        itemID = ConsumeItem_ID[entity];
        itemName = ConsumeItem_Name[entity];
        slotType = (InventorySlotType)Inventory_N[entity];
        tier = Tier[entity];
        maxCount = MaxCount[entity];
        canQuickSlot = Convert.ToBoolean(Quickable[entity]);
        canDiscard = Convert.ToBoolean(Discardable[entity]);
        canStorage = Convert.ToBoolean(Storageable[entity]);
        canUseable = Convert.ToBoolean(Useable[entity]);
        description = Description[entity];
        worldPrefabAddress = Addressable_Key[entity];

        // 아이콘은 Addressable로 비동기 로드
        LoadIconAsync();
        LoadPrefabAsync();
    }

    /// <summary>
    /// 소비 아이템 사용
    /// </summary>
    public override bool Apply(PlayerController player)
    {
        if (player == null) return false;

        int mainCat = M_Cat[entity];

        // 카테고리별 특수 처리
        switch ((ConsumableCategory)mainCat)
        {
            case ConsumableCategory.Food:
            case ConsumableCategory.Medical:
                return ApplyBasicConsumable(player);

            case ConsumableCategory.Jamming:
                return ApplyJamming(player);

            case ConsumableCategory.ExpansionChip:
                return ApplyExpansionChip(player);

            default:
                Debug.LogWarning($"정의되지 않은 소비 아이템 카테고리: {mainCat}");
                return false;
        }
    }
    // 기본 소비 (음식, 약)
    private bool ApplyBasicConsumable(PlayerController player)
    {
        float pollution = Pollution[entity];
        float thirst = Thirst[entity];
        float hunger = Hunger[entity];
        float hp = HP[entity];
        float temp = Temp[entity];

        player.playerStats.Health.Modify(hp);
        player.playerStats.Thirst.Modify(-thirst);
        player.playerStats.Hunger.Modify(-hunger);
        player.playerStats.Pollution.Modify(-pollution);
        player.playerStats.Temperature.Modify(temp);

        Debug.Log($"[소비] {itemName} - HP:{hp}, 오염:{pollution}");
        return true;
    }

    // 재밍 아이템
    private bool ApplyJamming(PlayerController player)
    {
        float range = Range[entity];
        float duration = Duration[entity];

        // 재밍 효과 적용
        Collider[] hits = Physics.OverlapSphere(player.transform.position, range, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            MechNPCController npc = hit.GetComponent<MechNPCController>();
            if (npc != null)
            {
                npc.ApplyStun(duration);
            }
        }

        Debug.Log($"[재밍] 범위:{range}m, 지속:{duration}초");
        player.Anim.SetJammingAnim();

        return true;
    }

    ///// <summary>
    ///// 아이템 사용 (소비)
    ///// </summary>
    //public override bool Apply(PlayerController player)
    //{
    //    if (player == null) return false;

    //    // 실시간으로 BG Database에서 최신 수치 읽기
    //    // (구글시트 수정 후 BG Database 동기화하면 자동 반영됨)
    //    float pollution = Pollution[entity];
    //    float thirst = Thirst[entity];
    //    float hunger = Hunger[entity];
    //    float hp = HP[entity];
    //    float temp = Temp[entity];

    //    //플레이어 스탯 적용
    //    player.playerStats.Health.Modify(hp);
    //    player.playerStats.Thirst.Modify(-thirst);
    //    player.playerStats.Hunger.Modify(-hunger);
    //    player.playerStats.Pollution.Modify(-pollution);
    //    player.playerStats.Temperature.Modify(temp);


    //    // 특수 효과 (재밍 아이템)
    //    int mainCat = M_Cat[entity];
    //    if (mainCat == (int)ConsumableCategory.Jamming)
    //    {
    //        float range = Range[entity];
    //        float duration = Duration[entity];
    //        //TODO: 재밍 펄스 생성
    //    }

    //    // VFX/SFX 재생
    //    // PlayUseEffect(player.transform.position);

    //    Debug.Log($"[아이템 사용] {itemName} - HP:{hp}, 오염도:{pollution}, 갈증:{thirst}, 허기:{hunger}, 체온:{temp}");
    //    return true;
    //}

    public bool ApplyExpansionChip(PlayerController playerController)
    {
        int mainCat = M_Cat[entity];
        if (mainCat == (int)ConsumableCategory.ExpansionChip)
        {
            bool success = playerController.ExpandInventory(Tier[entity]);
            return success;
        }
        return false;
    }
    /// <summary>
    /// Addressable로 아이콘 비동기 로드
    /// </summary>
    private async void LoadIconAsync()
    {
        //string path = ImgPath[entity];
        string path = "Icon/ConsumableIcon"; // 임시 경로

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
                    Debug.Log($"[ConsumableItemData] 아이콘 로드 성공 (자식): {itemName}");
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
    private async void LoadPrefabAsync()
    {
        string path = Addressable_Key[entity];

        // 경로가 비어있으면 기본 사용
        if (string.IsNullOrEmpty(path))
        {
            path = "Item/Item00";
        }

        // 먼저 지정된 경로 시도
        if (!await LoadPrefabFromPath(path))
        {
            // 실패하면 기본 경로로 재시도
            if (path != "Item/Item00")
            {
                Debug.LogWarning($"[ConsumableItemData] {path} 실패, 기본 경로로 재시도");
                await LoadPrefabFromPath("Item/Item00");
            }
        }
    }

    private async System.Threading.Tasks.Task<bool> LoadPrefabFromPath(string path)
    {
        try
        {
            // InvalidKeyException을 조용히 처리
            var checkHandle = Addressables.LoadResourceLocationsAsync(path);
            var locations = await checkHandle.Task;

            if (locations == null || locations.Count == 0)
            {
                Debug.Log($"[ConsumableItemData] Addressable 키 '{path}'가 없음, 기본값 사용");
                Addressables.Release(checkHandle);
                return false;
            }
            Addressables.Release(checkHandle);

            // 실제 로드
            var handle = Addressables.LoadAssetAsync<GameObject>(path);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                itemPrefab = handle.Result;
                return true;
            }
        }
        catch (InvalidKeyException)
        {
            // InvalidKeyException은 조용히 처리 (스팸 방지)
            Debug.Log($"[ConsumableItemData] '{path}' 키 없음");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ConsumableItemData] 예외: {e.Message}");
        }

        return false;
    }

    /// <summary>
    /// 사용 효과 재생 (VFX/SFX)
    /// </summary>
    private void PlayUseEffect(Vector3 position)
    {
        // TODO: TA 작업 - VFX/SFX 시스템과 연동
        // VFXManager.I.Play("ItemUse_" + itemName, position);
        // SFXManager.I.Play("ItemUse_Sound");
    }
}