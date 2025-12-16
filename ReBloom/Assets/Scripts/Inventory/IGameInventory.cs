using UnityEngine;

// ========================================
// 플레이어 인벤토리 전용 (게임 로직 포함)
// ========================================
public interface IGameInventory
{
    // 외부 공개 API만 정의
    int AddItemFromWorld(int itemID, int count);
    bool TryAddItemFromWorld(int itemID, int count);
    bool CanUnequip(int itemID);
    bool UseItem(int itemID);
    int GetItemCount(int itemID);
}