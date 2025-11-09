using UnityEngine;
using UnityEditor;

public class CreateStatsData
{
    [MenuItem("Tools/Create Stats Data")]
    static void CreateAsset()
    {
        StatsData asset = ScriptableObject.CreateInstance<StatsData>();
        
        // 기본값 설정
        asset.maxHealth = 100f;
        asset.hungerMax = 100f;
        asset.thurstMax = 100f;
        asset.pollutionMax = 100f;
        asset.hungerIncreaseRate = 0.5f;
        asset.thirstIncreaseRate = 0.5f;
        asset.normalTemperature = 36.5f;
        asset.pollutionIncreaseRate = 0.5f;

        AssetDatabase.CreateAsset(asset, "Assets/Data/PlayerStatsData.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("PlayerStatsData created at Assets/Data/PlayerStatsData.asset");
    }
}