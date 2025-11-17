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
    AbandonedSchool = 41,   // ��
    DepartmentStore = 42,   // ��ȭ��
    Factory = 43,           // ����
}


// =====================================================
// ==================== ������ ���� ====================
// =====================================================

public enum ItemTableType
{
    Consumable,
    Protective,
    Tool,
    Misc
}

/// <summary>
/// �κ��丮 ���� ��ġ (���?/�Һ�/��Ÿ/�߿�)
/// </summary>
public enum InventorySlotType
{
    Equipment = 0,      // ���? ��
    Consumable = 1,     // �Һ� ������ ��
    Misc = 2,           // ��Ÿ ������ ��
    Important = 3       // �߿� ������ ��
}

/// <summary>
/// ������ Ƽ�� (3�ܰ�)
/// </summary>
public enum ItemTier
{
    Common = 1,         // �Ϲ� (1�ܰ�)
    Rare = 2,           // ���? (2�ܰ�)
    Epic = 3            // ���� (3�ܰ�)
}

// ==================== �Һ� ������ ���� ====================

/// <summary>
/// �Һ� ������ ��з�?
/// </summary>
public enum ConsumableCategory
{
    Food = 1,           // 1. �ķ�
    Medical = 2,        // 2. ���޹�ǰ
    Jamming = 3          // 3. ���? ������ (���ļ�)
}

/// <summary>
/// �Һ� ������ �Һз�
/// </summary>
public enum ConsumableSubCategory
{
    CannedFood = 0,             // ������
    Water = 1,                  // ��
    Antidote = 2,               // ���� �ص�
    MedicalKit = 3,             // ���޻���
    CultivatedVegetable = 4,    // ���? ä��
    HeatPack = 5,               // �߿���
    Jammer = 6                  // ���?
}

/// <summary>
/// ������ (4�ܰ�)
/// �����ʹ� float��
/// ui �� �̺�Ʈ �뵵 (����)
/// </summary>
public enum ContaminationLevel
{
    None = 0,           // 0�ܰ� - ���� ����
    Low = 1,            // 1�ܰ� - ����
    Medium = 2,         // 2�ܰ� - �߰�
    High = 3            // 3�ܰ� - ����
}

// ==================== ���� ���� ====================

/// <summary>
/// ���� ���? ���?
/// </summary>
public enum ToolUsageType
{
    Plant = 1,              // 1. �Ĺ� (��)
    BuildingMineral = 2,    // 2. �ǹ�, ���� (���)
    All = 3                 // 3. �ǹ�, ����, �ڵ��� (��ġ)
}

/// <summary>
/// ���� ī�װ���
/// </summary>
public enum ToolCategory
{
    Shovel = 1,     // ��
    Pickaxe = 2,    // ���
    Bag = 3         // ����
    //��ġ??
}

// ==================== ��ȣ�� ���� ====================

/// <summary>
/// ��ȣ�� ����
/// </summary>
public enum ProtectiveGearType
{
    Clothing = 1,       // 1. ��
    Shoes = 2,           // 2. �Ź�
    None = 3             // 3. ����
}

// ==================== ��Ÿ ������ ���� ====================

/// <summary>
/// ��Ÿ ������ �з�
/// </summary>
public enum MiscItemCategory
{
    UnidentifiedSeed = 1,   // 1. ��Ȯ�� ����
    Seed = 2,              // 2. ����
    NaturalMaterial = 3,   // 3. �ڿ����?
    ProcessedMaterial = 4, // 4. �������?
    ImportantItem = 5      // 5. �߿� ������
}

/// <summary>
/// ���̵� ���? (���? ������ ���?)
/// ���� ���� ���̵� + ���� ���̵��� ����Ͽ�? ���?
/// </summary>
public enum MutationLevel
{
    VeryLow = 0,        // �ſ� ����
    Low = 1,            // ����
    Medium = 2,         // ����
    High = 3,           // ����
}

// =====================================================
// ==================== �κ��丮 ���� ====================
// =====================================================

/// <summary>
/// ���� �ɼ�
/// </summary>
public enum SortOption
{
    ByID,           // ID ��
    ByName,         // �̸� ��
    ByTier,         // Ƽ�� ��
    BySubCategory   // �Һз� ��
}

// ==================== ���� ���� ====================
public enum WeatherType
{
    Sunny,    // ����
    Rain,     // ��
    Radio,    // ����
    Snow,     // ��
    Thunder,  // õ��
    Hot       // ����
}
