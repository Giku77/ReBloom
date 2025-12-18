using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공용 컨테이너 슬롯 UI (인벤토리, 창고 등에서 재사용)
/// 슬롯 표시만 담당 - 비즈니스 로직 없음
/// </summary>
public class ContainerSlotsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private GameObject emptySlotPrefab;

    [Header("Settings")]
    [SerializeField] private DropZoneType zoneType = DropZoneType.Inventory;
    [SerializeField] private int slotPriority = 50;

    private InventoryItemData boundData;
    private readonly List<Transform> emptySlotList = new();
    private readonly List<GameInventorySlot> activeSlots = new();
    private int lastSlotCount = -1;

    #region Public API

    /// <summary>
    /// 데이터 바인딩 (외부에서 호출)
    /// </summary>
    public void Bind(InventoryItemData data)
    {
        // 기존 구독 해제
        if (boundData != null)
            boundData.OnContainerChanged -= RefreshUI;

        boundData = data;

        if (boundData != null)
        {
            boundData.OnContainerChanged += RefreshUI;
            CreateEmptySlots();
            RefreshUI();
            Debug.Log($"[ContainerSlotsUI] 바인딩 완료 - 슬롯: {boundData.SlotCount}개");
        }
    }

    /// <summary>
    /// 데이터 바인딩 해제
    /// </summary>
    public void Unbind()
    {
        if (boundData != null)
        {
            boundData.OnContainerChanged -= RefreshUI;
            Debug.Log("[ContainerSlotsUI] 바인딩 해제");
        }
        boundData = null;
    }

    /// <summary>
    /// 수동 새로고침
    /// </summary>
    public void ForceRefresh()
    {
        if (boundData != null)
            RefreshUI();
    }

    #endregion

    #region Unity 생명주기

    private void OnDestroy()
    {
        Unbind();
    }

    private void OnDisable()
    {
        // 비활성화 시에도 구독은 유지 (Unbind는 명시적으로만)
    }

    #endregion

    #region 슬롯 생성

    /// <summary>
    /// 빈 슬롯 생성 (DropZone 포함)
    /// </summary>
    private void CreateEmptySlots()
    {
        if (contentContainer == null)
        {
            Debug.LogError("[ContainerSlotsUI] contentContainer가 할당되지 않았습니다!");
            return;
        }

        if (emptySlotPrefab == null)
        {
            Debug.LogError("[ContainerSlotsUI] emptySlotPrefab이 할당되지 않았습니다!");
            return;
        }

        int slotCount = boundData.SlotCount;

        // 슬롯 수가 변경되지 않았으면 재생성 스킵
        if (lastSlotCount == slotCount && emptySlotList.Count == slotCount)
            return;

        // 기존 슬롯 정리
        ClearAllSlots();

        // 새 슬롯 생성
        for (int i = 0; i < slotCount; i++)
        {
            var slotObj = Instantiate(emptySlotPrefab, contentContainer);
            slotObj.transform.localScale = Vector3.one;
            slotObj.name = $"Slot_{i:D2}";

            // DropZoneMarker 설정
            var dropZone = slotObj.GetComponent<DropZoneMarker>();
            if (dropZone != null)
            {
                dropZone.SetZoneType(zoneType);
                dropZone.SetSlotIndex(i);
                dropZone.SetPriority(slotPriority);
            }
            else
            {
                Debug.LogWarning($"[ContainerSlotsUI] Slot_{i}에 DropZoneMarker가 없습니다!");
            }

            // 잠금 슬롯 마커 비활성화
            var deactivateMarker = slotObj.GetComponentInChildren<DeactivateSlotMarker>(true);
            var lockMarker = slotObj.GetComponentInChildren<LockImageMarker>(true);
            if (deactivateMarker != null) deactivateMarker.gameObject.SetActive(false);
            if (lockMarker != null) lockMarker.gameObject.SetActive(false);

            slotObj.SetActive(true);
            emptySlotList.Add(slotObj.transform);
        }

        lastSlotCount = slotCount;
        Debug.Log($"[ContainerSlotsUI] {slotCount}개 빈 슬롯 생성 완료");
    }

    #endregion

    #region UI 갱신

    /// <summary>
    /// UI 새로고침
    /// </summary>
    private void RefreshUI()
    {
        if (boundData == null || ItemDatabase.I == null)
        {
            Debug.LogWarning("[ContainerSlotsUI] boundData 또는 ItemDatabase가 없습니다.");
            return;
        }

        // 슬롯 수 변경 체크
        if (lastSlotCount != boundData.SlotCount)
        {
            CreateEmptySlots();
        }

        // 기존 아이템 슬롯 정리
        ClearItemSlots();

        // 슬롯 데이터 가져오기
        var slots = boundData.GetAllSlots();

        // 인덱스 기반 배치
        for (int i = 0; i < slots.Length && i < emptySlotList.Count; i++)
        {
            if (slots[i] != null && slots[i].itemID > 0)
            {
                ItemBase item = ItemDatabase.I.GetItem(slots[i].itemID);
                if (item != null)
                {
                    CreateItemSlot(item, slots[i].count, i);
                }
            }
        }
    }

    /// <summary>
    /// 아이템 슬롯만 정리 (EmptySlot은 유지)
    /// </summary>
    private void ClearItemSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        activeSlots.Clear();
    }

    /// <summary>
    /// 모든 슬롯 정리 (EmptySlot 포함)
    /// </summary>
    private void ClearAllSlots()
    {
        ClearItemSlots();

        foreach (var emptySlot in emptySlotList)
        {
            if (emptySlot != null)
                Destroy(emptySlot.gameObject);
        }
        emptySlotList.Clear();
        lastSlotCount = -1;
    }

    /// <summary>
    /// 아이템 슬롯 생성
    /// </summary>
    private void CreateItemSlot(ItemBase item, int quantity, int slotIndex)
    {
        if (itemSlotPrefab == null)
        {
            Debug.LogError("[ContainerSlotsUI] itemSlotPrefab이 할당되지 않았습니다!");
            return;
        }

        if (slotIndex >= emptySlotList.Count)
        {
            Debug.LogError($"[ContainerSlotsUI] 슬롯 인덱스 초과: {slotIndex}");
            return;
        }

        // EmptySlot의 자식으로 아이템 슬롯 생성
        var slotObj = Instantiate(itemSlotPrefab, emptySlotList[slotIndex]);
        slotObj.transform.localScale = Vector3.one;
        slotObj.name = $"ItemSlot_{item.itemID}";

        if (!slotObj.TryGetComponent(out GameInventorySlot slot))
        {
            Debug.LogError("[ContainerSlotsUI] GameInventorySlot 컴포넌트를 찾을 수 없습니다!");
            Destroy(slotObj);
            return;
        }

        // 아이템 설정
        slot.SetItem(item, quantity);

        // 드래그 핸들러 설정
        if (slotObj.TryGetComponent(out ItemIconDragHandler dragHandler))
        {
            dragHandler.SetItemData(item);
        }

        activeSlots.Add(slot);
        slotObj.SetActive(true);
    }

    #endregion
}