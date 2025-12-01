using UnityEngine;
using System;

/// <summary>
/// 게임 전역 인벤토리 이벤트 시스템
/// UI 상태, 플레이어 행동 등 전역 이벤트만 관리
/// </summary>
public static class InventroyEventSystem
{
    // ---- UI 상태 이벤트 ----
    public static event Action OnInventoryOpened;
    public static event Action OnInventoryClosed;

    // ---- 게임플레이 이벤트 (로봇 펫이 반응) ----
    public static event Action OnInventoryFull;
    public static event Action OnItemDropped;
    public static event Action<int> OnItemAcquired;  // Tier 정보 포함

    // ---- 이벤트 호출 메서드 ----
    public static void InventoryOpened() => OnInventoryOpened?.Invoke();
    public static void InventoryClosed() => OnInventoryClosed?.Invoke();
    public static void InventoryFull() => OnInventoryFull?.Invoke();
    public static void ItemDropped() => OnItemDropped?.Invoke();
    public static void ItemAcquired(int tier) => OnItemAcquired?.Invoke(tier);

    // ---- 이벤트 정리 (씬 전환 시) ----
    public static void ClearAllEvents()
    {
        OnInventoryOpened = null;
        OnInventoryClosed = null;
        OnInventoryFull = null;
        OnItemDropped = null;
        OnItemAcquired = null;
    }
}