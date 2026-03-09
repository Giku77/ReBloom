using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveConstants
{
    public const int SAVE_VERSION = 1;
}

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
    public List<MultiplayerPlayerSaveDTO> multiplayerPlayers = new List<MultiplayerPlayerSaveDTO>();
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
    public int resW;
    public int resH;
}

[Serializable]
public class SaveMetaDTO
{
    public int version = SaveConstants.SAVE_VERSION;
    public string slotId = "slot1";
    public string displayName;
    public string hostPlayFabId;
    public long createdAtUtcTicks;
    public long savedAtUtcTicks;
    public string sceneName;
    public string commitId;
}

[Serializable]
public class WorldSlotMetaDTO
{
    public string slotId;
    public string displayName;
    public string hostPlayFabId;
    public string sceneName;
    public string commitId;
    public long createdAtUtcTicks;
    public long lastSavedAtUtcTicks;
}

[Serializable]
public class SaveSlotIndexDTO
{
    public long updatedAtUtcTicks;
    public List<WorldSlotMetaDTO> slots = new List<WorldSlotMetaDTO>();
}

[Serializable]
public class TutorialSaveDTO
{
    public bool introCompleted;
    public int resumeTutorialId;
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
    public int clothItemId;
    public int shoesItemId;
    public int toolItemId;
}

[Serializable]
public class QuestSaveDTO
{
    public int currentQuestId;
    public bool firstQuestCompleted;
}

[Serializable]
public class EnvironmentSaveDTO
{
    public int currentStageId;
    public int day;
    public int hour;
    public int minute;
    public WeatherType weather;
    public float weatherDuration;
    public float weatherTimer;
    public float currentPollution;
    public float currentThirst;
    public float currentTemp;
}

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
    public bool isDead;
}

[Serializable]
public class MultiplayerPlayerSaveDTO
{
    public string persistentPlayerId;
    public string displayName;
    public TransformDTO transform = new TransformDTO();
    public float hp;
    public float hunger;
    public float thirst;
    public float pollution;
    public float temperature;
    public bool isDead;
    public EquipmentSaveDTO equipment = new EquipmentSaveDTO();
    public int inventoryTier;
    public int inventoryCapacity;
    public List<ItemSlotDTO> inventoryItems = new List<ItemSlotDTO>();
}

[Serializable]
public class WorldSaveDTO
{
    public List<BuildingInstanceSaveDTO> placedBuildings = new List<BuildingInstanceSaveDTO>();
    public List<ContainerSaveDTO> containers = new List<ContainerSaveDTO>();
    public List<int> visitedStages = new List<int>();
    public List<string> destroyedKeys = new List<string>();
    public List<FarmBedSaveDTO> farmBeds = new List<FarmBedSaveDTO>();
    public List<GreenhouseUpgradeSaveDTO> greenhouseUpgrades = new List<GreenhouseUpgradeSaveDTO>();
}

[Serializable]
public class BuildingInstanceSaveDTO
{
    public string guid;
    public int prefabId;
    public TransformDTO transform = new TransformDTO();
    public string containerGuid;
}

[Serializable]
public class ContainerSaveDTO
{
    public string guid;
    public int capacity;
    public List<ItemSlotDTO> items = new List<ItemSlotDTO>();
}

[Serializable]
public class FarmBedSaveDTO
{
    public string guid;
    public List<FarmSlotSaveDTO> slots = new List<FarmSlotSaveDTO>();
}

[Serializable]
public class FarmSlotSaveDTO
{
    public int state;
    public int cropId;
    public int stageIndex;
    public float stageTimer;
    public int wateredCount;
    public float fertilizerRemain;
    public float growSpeedMultiplier;
}

[Serializable]
public class GreenhouseUpgradeSaveDTO
{
    public string greenhouseId;
    public List<GreenhouseUpgradeProgressDTO> progress = new List<GreenhouseUpgradeProgressDTO>();
}

[Serializable]
public class GreenhouseUpgradeProgressDTO
{
    public int sort;
    public int completedGrade;
}

[Serializable]
public class ItemSlotDTO
{
    public int slot;
    public int itemId;
    public int amount;
    public int durability;
    public string extraJson;
}

[Serializable]
public class TransformDTO
{
    public float px, py, pz;
    public float rx, ry, rz;
    public float sx, sy, sz;

    public static TransformDTO From(Transform t)
    {
        var p = t.position;
        var r = t.eulerAngles;
        var s = t.localScale;
        return new TransformDTO
        {
            px = p.x,
            py = p.y,
            pz = p.z,
            rx = r.x,
            ry = r.y,
            rz = r.z,
            sx = s.x,
            sy = s.y,
            sz = s.z
        };
    }

    public void ApplyTo(Transform t)
    {
        t.position = new Vector3(px, py, pz);
        t.eulerAngles = new Vector3(rx, ry, rz);
        t.localScale = new Vector3(sx, sy, sz);
    }
}
