using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveConstants
{
    public const int SAVE_VERSION = 1;
}

// ---- Root ----
[Serializable]
public class SaveGameDTO
{
    public SaveMetaDTO meta = new SaveMetaDTO();
    public PlayerSaveDTO player = new PlayerSaveDTO();
    public WorldSaveDTO world = new WorldSaveDTO();
    public QuestSaveDTO quest = new QuestSaveDTO();
    public EnvironmentSaveDTO env = new EnvironmentSaveDTO();
    public ResearchSaveDTO research = new ResearchSaveDTO();
    public TutorialSaveDTO tutorial = new TutorialSaveDTO();
    public CutSceneSaveDTO cutScene = new CutSceneSaveDTO();

    public SettingsDTO settings = new SettingsDTO();
}

[Serializable]
public class SettingsDTO
{
    public float master = 1f;
    public float bgm = 0.5f;
    public float sfx = 1f;

    public bool fullscreen = true;
    public bool vsync = true;
    public float mouseSensitivity = 3f;

    public int graphicsQuality = 0;
    public int targetFrameRate = 120;
    public int poppyVoiceType = 1;

    // 해상도는 플랫폼/모니터에 따라 달라서 “원시값”으로 저장
    public int resW;
    public int resH;
}



[Serializable]
public class SaveMetaDTO
{
    public int version = SaveConstants.SAVE_VERSION;
    public string slotId = "slot1";
    public long savedAtUtcTicks;
    public string sceneName;
    public string commitId; // 부분 저장 방지/디버깅용
}

[Serializable]
public class TutorialSaveDTO
{
    public bool introCompleted;
    public int resumeTutorialId; // 0이면 완료/없음
}

[Serializable]
public class ResearchSaveDTO
{
    public float energy;
    public float progress;
    public float greening;
}

[Serializable]
public class CutSceneSaveDTO
{
    public bool introCutsceneSeen;
}

[Serializable]
public class EquipmentSaveDTO
{
    public int clothItemId; // 0이면 없음
    public int shoesItemId;
    public int toolItemId;
}

[Serializable]
public class QuestSaveDTO
{
    public int currentQuestId; // 0이면 없음
    public bool firstQuestCompleted;
    public bool endingPlayed;
    public List<QuestGoalProgressDTO> goalProgress = new List<QuestGoalProgressDTO>();
}

[Serializable]
public class QuestGoalProgressDTO
{
    public int goalIndex;
    public QuestGoalType type;
    public int objectId;
    public int currentCount;
}

[Serializable]
public class EnvironmentSaveDTO
{
    // 지역
    public int currentStageId;

    // 시간
    public int day;
    public int hour;
    public int minute;

    // 날씨(“현재 지역” 기준으로만 저장해도 UI는 충분히 복원됨)
    public WeatherType weather;
    public float weatherDuration;
    public float weatherTimer;

    public float currentPollution;
    public float currentThirst;
    public float currentTemp;
}

// ---- Player ----
[Serializable]
public class PlayerSaveDTO
{
    public TransformDTO transform = new TransformDTO();
    public string inventoryContainerGuid;
    public float hp;
    public float hunger;
    public float thirst;
    public float pollution;
    public float temperature;

    public EquipmentSaveDTO equipment = new EquipmentSaveDTO();

    // 죽음 상태/디버프/장비 등
    public bool isDead;
}

// ---- World ----
[Serializable]
public class WorldSaveDTO
{
    public List<BuildingInstanceSaveDTO> placedBuildings = new List<BuildingInstanceSaveDTO>();

    // 컨테이너(창고/시체박스/상자/보관함 등) 데이터는 여기로 분리
    public List<ContainerSaveDTO> containers = new List<ContainerSaveDTO>();

    public List<int> visitedStages = new();

    public List<string> destroyedKeys = new List<string>();

    // TODO: farms / incubators / quests 등도 여기에 확장
}

[Serializable]
public class BuildingInstanceSaveDTO
{
    public string guid;
    public int prefabId;
    public TransformDTO transform = new TransformDTO();

    // 이 건축물이 “보관함”을 가진다면 컨테이너 GUID로 연결
    public string containerGuid;
}

[Serializable]
public class ContainerSaveDTO
{
    public string guid;
    public int capacity;
    public List<ItemSlotDTO> items = new List<ItemSlotDTO>();
}

// 슬롯 기반 (인덱스 + 아이템 스택)
[Serializable]
public class ItemSlotDTO
{
    public int slot;
    public int itemId;
    public int amount;

    // 선택: 내구도/커스텀 데이터 등
    public int durability;
    public string extraJson;
}

[Serializable]
public class TransformDTO
{
    public float px, py, pz;
    public float rx, ry, rz; // euler
    public float sx, sy, sz;

    public static TransformDTO From(Transform t)
    {
        var p = t.position;
        var r = t.eulerAngles;
        var s = t.localScale;
        return new TransformDTO
        {
            px = p.x, py = p.y, pz = p.z,
            rx = r.x, ry = r.y, rz = r.z,
            sx = s.x, sy = s.y, sz = s.z
        };
    }

    public void ApplyTo(Transform t)
    {
        t.position = new Vector3(px, py, pz);
        t.eulerAngles = new Vector3(rx, ry, rz);
        t.localScale = new Vector3(sx, sy, sz);
    }
}

