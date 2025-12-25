public enum QuestIds
{
    Quest_001,
    Quest_002,
    Quest_003,
    Quest_004,
    Quest_005,
    Quest_006,
    Quest_007,
    Quest_008,
    Quest_009,
    Quest_010,
    Quest_011,
}


public enum EntranceType
{
    Shelter = 400, // 거점
    AbandonedSchool = 401,   // 폐교
    DepartmentStore = 402,   // 백화점
    Factory = 403,           // 공장
}


// ==================== 날씨 타입 ====================
public enum WeatherType
{
    Sunny,    // 맑음
    Rain,     // 비
    Radio,    // Radio
    Snow,     // 눈
    Thunder,  // 천둥 (번개)
    Hot       // 더움
}

public enum DayCycle
{
    Dawn,    // 일출 (05~07)
    Morning, // 아침 (07~11)
    Day,     // 낮 (11~17)
    Dusk,    // 일몰 (17~19)
    Night    // 밤 (19~05)
}

public enum UIType
{
    None,
    Inventory,
    InventoryStats,
    Crafting,
    Stats,
    Equipment,
    Quest,
    Building,
    Weather,
    Debug,
    QuickSlot,
    Dialogue,
    PlayerEffect,
    EditBuild,
    WaterTank,
    Farm,
    Cultivation,
    GamePause,
    Setting,
    Storage,
    MobileMain,
    FarmUpgrade,
    SeedPurifier
}

public enum UILayer
{
    HUD,
    Modal,
    Overlay,
    MobileHUD,
    MobileModal,
}