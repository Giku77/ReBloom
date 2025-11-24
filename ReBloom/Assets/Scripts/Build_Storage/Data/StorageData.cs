// 창고 건축물 - 용량 제한, 건물 ID 추가
using UnityEngine;

[CreateAssetMenu(fileName = "Storage", menuName = "ReBloom/Container/Storage")]
public class StorageData : ItemContainerBase
{
    [Header("Storage Info")]
    [SerializeField] private string storageID;
    [SerializeField] private int storageTier = 1; // 티어에 따라 용량 다름

    public System.Action OnStorageChanged;
    public string StorageID => storageID;

    // 티어별 슬롯 수 오버라이드
    public override int SlotCount => storageTier switch
    {
        1 => 10,
        2 => 20,
        3 => 30,
        _ => 10
    };
}