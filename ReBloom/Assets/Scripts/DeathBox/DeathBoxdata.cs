using System;
using UnityEditor.Rendering;
using UnityEngine;

[CreateAssetMenu(fileName = "DeathBox", menuName = "ReBloom/Container/DeathBox")]
public class DeathBoxData : ItemContainerBase
{
    [Header("Death Info")]
    [SerializeField] private string deathBoxID;
    [SerializeField] private Vector3 deathPosition;
    [SerializeField] private DateTime deathTime;

    public string DeathBoxID => deathBoxID;
    public Vector3 DeathPosition => deathPosition;
    public DateTime DeathTime => deathTime;

    // 시체박스 전용: 인벤토리에서 아이템 받아오기

    /// <summary>
    /// 메타데이터만 설정 (아이템 Clear 없이)
    /// </summary>
    public void SetMetadata(Vector3 position)
    {
        deathBoxID = Guid.NewGuid().ToString();
        deathPosition = position;
        deathTime = DateTime.Now;
    }

    /// <summary>
    /// 전체 초기화 (Clear 포함)
    /// </summary>
    public void InitializeEmpty(Vector3 position)
    {
        Clear();
        SetMetadata(position);
    }

    /// <summary>
    /// 인벤토리에서 아이템 받기 (Clear 없이)
    /// </summary>
    public void AddItemsFromInventory(IItemContainer inventory)
    {
        bool success = ItemTransferUtility.TransferAll(inventory, this);
        Debug.Log($"[DeathBox] 인벤토리에서 이전 결과: {success}");
    }
    //public void CreateFromInventory(IItemContainer inventory, Vector3 position)
    //{
    //    //Clear();

    //    SetMetadata(position);

    //    ItemTransferUtility.TransferAll(inventory, this);

    //    Debug.Log($"[DeathBox] 위치 {position}에 시체박스 생성됨");
    //}
}

/// <summary>
/// 슬롯 데이터
/// </summary>
[Serializable]
public class ItemSlotData
{
    public int itemID;
    public int count;
}