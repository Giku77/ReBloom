//WARN: keep the file outside of asmdef folder, otherwise it'll fail to compile as editor depends on a Runtime lib which in turn is only added if symbol is defined, that's to ensure child package is compiled correctly
#if !LiveScriptReload_Enabled && UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveScriptReload.Scripts.Editor
{
  [InitializeOnLoad]
  public class EnsurePreprocessorDirectiveAdded : MonoBehaviour
  {
    public static string BuildSymbol_LiveScriptReload = "LiveScriptReload_Enabled";

    static EnsurePreprocessorDirectiveAdded()
    {
      Debug.LogError($"'{BuildSymbol_LiveScriptReload}' define symbol not present. " +
                     "Live Script Reload will not work correctly (will try to auto-add), if you can still see error after please add via:" +
                     "\r\n1) Edit -> Project Settings -> Player -> Scripting Define Symbols" +
                     $"2) Ensure '{BuildSymbol_LiveScriptReload}' is added for all platforms that you intend to use the tool on, eg Windows / Android");

      BuildDefineSymbolManager.SetBuildDefineSymbolState(BuildSymbol_LiveScriptReload, true);
    }

    // From ImmersiveVRTools.Editor.Common - EnsurePreprocessorDirectiveAdded is not in main asmdef and helper dll is not auto-reference
    public static class BuildDefineSymbolManager
    {
      private const string ProductSymbolPrefix = "PRODUCT_";

      public static void SetBuildDefineSymbolState(string buildSymbol, bool isEnabled)
      {
        SetBuildDefineSymbolState(buildSymbol, isEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
      }

      public static void SetBuildDefineSymbolState(
        string buildSymbol,
        bool isEnabled,
        BuildTargetGroup buildTargetGroup)
      {
        var allBuildSymbols = GetAllBuildSymbols(buildTargetGroup);
        var flag = allBuildSymbols.Any(s => s == buildSymbol);
        if (isEnabled && !flag)
        {
          allBuildSymbols.Add(buildSymbol);
          SetBuildSymbols(allBuildSymbols, buildTargetGroup);
          Debug.Log("Build Symbol Added: " + buildSymbol);
        }

        if (!isEnabled & flag)
        {
          allBuildSymbols.Remove(buildSymbol);
          SetBuildSymbols(allBuildSymbols, buildTargetGroup);
          Debug.Log("Build Symbol Removed: " + buildSymbol);
        }

        EditorUtility.ClearProgressBar();
      }

      public static void SetProductSymbol(string productNameSymbol)
      {
        SetProductSymbol(productNameSymbol, EditorUserBuildSettings.selectedBuildTargetGroup);
      }

      public static void SetProductSymbol(string productNameSymbol, BuildTargetGroup buildTargetGroup)
      {
        if (!productNameSymbol.StartsWith("PRODUCT_"))
          throw new Exception("Product name symbol needs to start with: 'PRODUCT_'");
        var allBuildSymbols = GetAllBuildSymbols(buildTargetGroup);
        foreach (var buildSymbol in allBuildSymbols.Where(s => s.StartsWith("PRODUCT_")).ToList()
                   .Where(s => s != productNameSymbol))
          SetBuildDefineSymbolState(buildSymbol, false, buildTargetGroup);
        if (allBuildSymbols.Contains(productNameSymbol))
          return;
        SetBuildDefineSymbolState(productNameSymbol, true, buildTargetGroup);
      }

      private static List<string> GetAllBuildSymbols(BuildTargetGroup buildTargetGroup)
      {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList();
      }

      private static void SetBuildSymbols(
        List<string> allBuildSymbols,
        BuildTargetGroup buildTargetGroup)
      {
        EditorUtility.DisplayProgressBar("Please wait", "Modifying build symbols", 0.1f);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup,
                    string.Join(";", allBuildSymbols.ToArray()));
            }
        }
    }
}
#endif