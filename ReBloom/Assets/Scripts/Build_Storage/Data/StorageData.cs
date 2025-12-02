using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Storage", menuName = "ReBloom/Container/Storage")]
public class StorageData : ItemContainerBase
{
    [Header("Storage Info")]
    [SerializeField] private string storageID;
    [SerializeField] private int storageTier = 1;

    // UI 갱신용 이벤트
    public event Action OnStorageChanged;

    public string StorageID => storageID;
    public int StorageTier => storageTier;

    // 티어별 슬롯 수 오버라이드
    public override int SlotCount => storageTier switch
    {
        1 => 50,
        2 => 60,
        _ => 50
    };

    #region ItemContainerBase 오버라이드

    /// <summary>
    /// 아이템 추가 시 이벤트 발생
    /// </summary>
    public override bool AddItem(int itemID, int count)
    {
        bool success = base.AddItem(itemID, count);

        if (success)
        {
            OnStorageChanged?.Invoke();
            Debug.Log($"[StorageData] 아이템 추가: ID={itemID}, Count={count}");
        }

        return success;
    }

    /// <summary>
    /// 아이템 제거 시 이벤트 발생
    /// </summary>
    public override bool RemoveItem(int itemID, int count)
    {
        bool success = base.RemoveItem(itemID, count);

        if (success)
        {
            OnStorageChanged?.Invoke();
            Debug.Log($"[StorageData] 아이템 제거: ID={itemID}, Count={count}");
        }

        return success;
    }

    #endregion

    /// <summary>
    /// 외부에서 명시적으로 UI 갱신 요청 (TransferTo 완료 후)
    /// </summary>
    public void NotifyStorageChanged()
    {
        OnStorageChanged?.Invoke();
        Debug.Log("[StorageData] Storage 변경 알림 발생");
    }
}