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
    private void OnEnable()
    {
        OnContainerChanged += HandleContainerChanged;
    }

    private void OnDisable()
    {
        OnContainerChanged -= HandleContainerChanged;
    }

    private void HandleContainerChanged()
    {
        OnStorageChanged?.Invoke();
    }
    /// <summary>
    /// 외부에서 명시적으로 UI 갱신 요청 (TransferTo 완료 후) // 함수 수정 필요함
    /// </summary>
    public void NotifyStorageChanged()
    {
        OnStorageChanged?.Invoke();
        Debug.Log("[StorageData] Storage 변경 알림 발생");
    }
}