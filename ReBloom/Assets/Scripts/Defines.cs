enum QuestIds
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


// =====================================================
// ==================== 아이템 관련 ====================
// =====================================================

public enum ItemTableType 
{ 
    Consumable,
    Protective,
    Tool,
    Misc
}

/// <summary>
/// 인벤토리 슬롯 위치 (장비/소비/기타/중요)
/// </summary>
public enum InventorySlotType
{
    Equipment = 0,      // 장비 탭
    Consumable = 1,     // 소비 아이템 탭
    Misc = 2,           // 기타 아이템 탭
    Important = 3       // 중요 아이템 탭
}

/// <summary>
/// 아이템 티어 (3단계)
/// </summary>
public enum ItemTier
{
    Common = 1,         // 일반 (1단계)
    Rare = 2,           // 희귀 (2단계)
    Epic = 3            // 영웅 (3단계)
}

// ==================== 소비 아이템 관련 ====================

/// <summary>
/// 소비 아이템 대분류
/// </summary>
public enum ConsumableCategory
{
    Food = 1,           // 1. 식량
    Medical = 2,        // 2. 구급물품
    Jamming = 3          // 3. 재밍 아이템 (주파수)
}

/// <summary>
/// 소비 아이템 소분류
/// </summary>
public enum ConsumableSubCategory
{
    CannedFood = 0,             // 통조림
    Water = 1,                  // 물
    Antidote = 2,               // 방사능 해독
    MedicalKit = 3,             // 구급상자
    CultivatedVegetable = 4,    // 재배 채소
    HeatPack = 5,               // 발열팩
    Jammer = 6                  // 재머
}

/// <summary>
/// 오염도 (4단계)
/// 데이터는 float형
/// ui 및 이벤트 용도 (예비)
/// </summary>
public enum ContaminationLevel
{
    None = 0,           // 0단계 - 오염 없음
    Low = 1,            // 1단계 - 낮음
    Medium = 2,         // 2단계 - 중간
    High = 3            // 3단계 - 높음
}

// ==================== 도구 관련 ====================

/// <summary>
/// 도구 사용 장소
/// </summary>
public enum ToolUsageType
{
    Plant = 1,              // 1. 식물 (낫)
    BuildingMineral = 2,    // 2. 건물, 광물 (곡괭이)
    All = 3                 // 3. 건물, 광물, 자동차 (도끼)
}

// ==================== 보호구 관련 ====================

/// <summary>
/// 보호구 종류
/// </summary>
public enum ProtectiveGearType
{
    Clothing = 1,       // 1. 옷
    Shoes = 2,           // 2. 신발
    None = 3             // 3. 없음
}

// ==================== 기타 아이템 관련 ====================

/// <summary>
/// 기타 아이템 분류
/// </summary>
public enum MiscItemCategory
{
    UnidentifiedSeed = 1,   // 1. 미확인 종자
    Seed = 2,              // 2. 종자
    NaturalMaterial = 3,   // 3. 자연재료
    ProcessedMaterial = 4, // 4. 가공재료
    ImportantItem = 5      // 5. 중요 아이템
}

/// <summary>
/// 변이도 등급 (드랍 결정에 사용)
/// 지역 고유 변이도 + 날씨 변이도를 계산하여 사용
/// </summary>
public enum MutationLevel
{
    VeryLow = 0,        // 매우 낮음
    Low = 1,            // 낮음
    Medium = 2,         // 보통
    High = 3,           // 높음
}