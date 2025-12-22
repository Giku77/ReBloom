using Cysharp.Threading.Tasks;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ArcData
{
    public int arcId;
    public string name;
    public int tier;
    public int arcType;
    public float energyInc;
    public float energyDec;
    public float researchInc;
    public float greeningInc;
    public int unlockValue;
    public int installLimit;
    public float buildTime;
    public string text;
    public int arcInteraction;
    public float interactTime;
    public string interactText;
    public int arrangePossible;
    public GameObject buildPrefab;
    public GameObject previewPrefab;
    public string iconAddressableKey;
    public Sprite icon;

    public async UniTask LoadIconAsync()
    {
        // 1) buildPrefab null 체크
        if (buildPrefab == null)
        {
            Debug.LogWarning($"[ArcData] {name}: buildPrefab이 null");
            return; // 에러 안 던지고 그냥 종료
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("Assets/Rebloom_Arts/Icon/Building/").Append(buildPrefab.name).Append("_icon.png");
        string key = sb.ToString();

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[ArcData] {name}: iconPath가 비어있습니다");
            return;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            icon = await handle.ToUniTask();
        }
        catch (Exception e)
        {
            // 실패해도 에러 던지지 않음 - 기본 아이콘 사용
            Debug.LogWarning($"[ArcData] {name}: 아이콘 로드 실패 - {e.Message}");
            icon = null; // BuildSlotUI에서 defaultIcon 사용
        }
    }
}

    public class ArcContext
{
    public ArcData Data;       
    public GameObject ArcPrefab;
    public Vector3 Position;        
    public Quaternion Rotation;
    public BuildingFootprint FootPrint;
    public Transform PlayerTransform;

    public float DepthOffset;
    public BuildingInstance IgnoreOccupancyInstance;
}
