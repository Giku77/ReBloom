using System;
using System.Collections.Generic;

[Serializable]
public class GreenhouseUpgradeRowData
{
    public int upgradeId;
    public string upgradeName;

    public int sort;   // Upgrade_Sort
    public int grade;  // Upgrade_Grade

    public int function;       // Upgrade_Function
    public bool isApplyNewArc; // IsApply_New_Arc == 1

    public string activePrefab1; // Active_Prefab1
    public string activePrefab2; // Active_Prefab2
    public string activePrefab3; // Active_Prefab3

    public int needItem1;   // Need_Item1
    public int needCount1;  // Need_Item_C1
    public int needItem2;   // Need_Item2
    public int needCount2;  // Need_Item_C2

    public IEnumerable<string> ActiveKeys()
    {
        if (IsValid(activePrefab1)) yield return activePrefab1;
        if (IsValid(activePrefab2)) yield return activePrefab2;
        if (IsValid(activePrefab3)) yield return activePrefab3;

        static bool IsValid(string k) => !string.IsNullOrWhiteSpace(k) && k != "0";
    }

    public IEnumerable<(int itemId, int count)> Costs()
    {
        if (needItem1 != 0 && needCount1 > 0) yield return (needItem1, needCount1);
        if (needItem2 != 0 && needCount2 > 0) yield return (needItem2, needCount2);
    }
}
