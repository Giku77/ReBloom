using System;
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
    public void CreateFromInventory(IItemContainer inventory, Vector3 position)
    {
        Clear();

        deathBoxID = Guid.NewGuid().ToString();
        deathPosition = position;
        deathTime = DateTime.Now;

        ItemTransferUtility.TransferAll(inventory, this);

        Debug.Log($"[DeathBox] 위치 {position}에 시체박스 생성됨");
    }
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