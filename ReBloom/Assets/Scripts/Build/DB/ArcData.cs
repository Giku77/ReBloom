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
            Debug.LogWarning($"[ArcData] {name}: buildPrefab이 null입니다");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("Assets/Arts/Icon/Building/").Append(buildPrefab.name).Append("_icon.png");
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
            Debug.LogWarning($"[ArcData] {name}: 아이콘 로드 실패 폴백 시도- {key}");
            iconAddressableKey = "Building/Icon";

            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            icon = await handle.ToUniTask();
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
