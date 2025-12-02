using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DragDropManager (Marker 기반)
/// </summary>
public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private QuickSlot quickSlotManager;
    [SerializeField] private InventoryItemData inventoryData;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private RemovePopUp removePopUp;

    private DragContext currentDrag;
    private WorldStorage currentStorage;
    private DropZoneMarker currentHoveredZone;

    #region Singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Public Methods
    public void BeginDrag(DragContext context)
    {
        currentDrag = context;
        Debug.Log($"[DragDropManager] 드래그 시작: {context.Item.itemName}");
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (currentDrag == null) return;

        // RaycastAll로 검색
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 최적 드롭존 찾기 (우선순위 기반)
        DropZoneMarker bestZone = FindBestDropZone(results);

        // 처리
        if (bestZone != null && CanDrop(bestZone, currentDrag))
        {
            HandleDrop(bestZone, currentDrag);
            currentDrag.Source?.OnDragSuccess();
            Debug.Log($"[DragDropManager] 드롭 성공: {bestZone.ZoneType}");
        }
        else
        {
            currentDrag.Source?.OnDragCancelled();
            Debug.Log("[DragDropManager] 드롭 실패");
        }

        // 비주얼 초기화
        if (currentHoveredZone != null)
        {
            currentHoveredZone.ResetVisual();
            currentHoveredZone = null;
        }

        currentDrag = null;
    }

    /// <summary>
    /// 호버 처리 (비주얼 피드백)
    /// </summary>
    public void NotifyHover(PointerEventData eventData)
    {
        if (currentDrag == null) return;

        // RaycastAll
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DropZoneMarker hoveredZone = FindBestDropZone(results);

        // 이전 호버 초기화
        if (currentHoveredZone != hoveredZone)
        {
            currentHoveredZone?.ResetVisual();
            currentHoveredZone = hoveredZone;
        }

        // 새 호버 표시
        if (hoveredZone != null)
        {
            if (CanDrop(hoveredZone, currentDrag))
            {
                hoveredZone.ShowValidHover();
            }
            else
            {
                hoveredZone.ShowInvalidHover();
            }
        }
    }

    public void SetCurrentStorage(WorldStorage storage)
    {
        currentStorage = storage;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 우선순위 기반 드롭존 선택
    /// </summary>
    private DropZoneMarker FindBestDropZone(List<RaycastResult> results)
    {
        DropZoneMarker bestZone = null;
        int highestPriority = -1;

        foreach (var result in results)
        {
            var zone = result.gameObject.GetComponent<DropZoneMarker>();
            if (zone != null && zone.Priority > highestPriority)
            {
                highestPriority = zone.Priority;
                bestZone = zone;
            }
        }

        return bestZone;
    }

    /// <summary>
    /// 드롭 가능 여부 검증
    /// </summary>
    private bool CanDrop(DropZoneMarker zone, DragContext context)
    {
        switch (zone.ZoneType)
        {
            case DropZoneType.QuickSlot:
                return CanDropToQuickSlot(zone, context);

            case DropZoneType.Storage:
                return context.IsFromInventory && currentStorage != null;

            case DropZoneType.World:
                return true;

            case DropZoneType.Inventory:
                return true;

            default:
                return false;
        }
    }

    private bool CanDropToQuickSlot(DropZoneMarker zone, DragContext context)
    {
        if (!context.Item.canQuickSlot) return false;
        if (context.IsFromQuickSlot && context.SourceSlotIndex == zone.SlotIndex) return false;
        return true;
    }

    /// <summary>
    /// 실제 드롭 처리
    /// </summary>
    private void HandleDrop(DropZoneMarker zone, DragContext context)
    {
        switch (zone.ZoneType)
        {
            case DropZoneType.QuickSlot:
                HandleQuickSlotDrop(zone.SlotIndex, context);
                break;

            case DropZoneType.Storage:
                HandleStorageDrop(context);
                break;

            case DropZoneType.World:
                HandleWorldDrop(context);
                break;

            case DropZoneType.Inventory:
                HandleInventoryDrop(zone.SlotIndex, context);
                break;
        }
    }

    private void HandleQuickSlotDrop(int targetSlot, DragContext context)
    {
        if (context.IsFromQuickSlot)
        {
            SwapQuickSlots(context.SourceSlotIndex, targetSlot);
        }
        else
        {
            AssignToQuickSlot(targetSlot, context.Item);
        }
    }

    private void HandleStorageDrop(DragContext context)
    {
        int quantity = inventoryData.GetItemCount(context.Item.itemID);
        if (inventoryData.RemoveItem(context.Item.itemID, quantity))
        {
            currentStorage.AddItem(context.Item, quantity);
        }
    }

    private void HandleWorldDrop(DragContext context)
    {
        if (context.IsFromDebug)
        {
            DropToWorld(context.Item, 1);
        }
        else if (context.IsFromQuickSlot)
        {
            quickSlotManager.RemoveSlot(context.SourceSlotIndex);
        }
        else
        {
            removePopUp?.OnOpen(context.Item);
        }
    }

    private void HandleInventoryDrop(int targetSlot, DragContext context)
    {
        // TODO: 인벤토리 간 이동
    }
    #endregion

    #region Helper Methods (기존 코드)
    private void SwapQuickSlots(int from, int to)
    {
        // ... (기존 코드)
    }

    private void AssignToQuickSlot(int slot, ItemBase item)
    {
        // ... (기존 코드)
    }

    private async void DropToWorld(ItemBase item, int quantity)
    {
        // ... (기존 코드)
    }
    #endregion
}