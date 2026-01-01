#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrefabMissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts in Selection")]
    public static void FindInSelection()
    {
        var obj = Selection.activeObject;
        if (obj == null)
        {
            Debug.LogWarning("[MissingScriptFinder] Nothing selected.");
            return;
        }

        // 1) Project에서 Prefab 에셋 선택한 경우
        var prefabAsset = obj as GameObject;
        if (prefabAsset != null && PrefabUtility.GetPrefabAssetType(prefabAsset) != PrefabAssetType.NotAPrefab)
        {
            ScanPrefabAsset(prefabAsset);
            return;
        }

        // 2) Hierarchy에서 GameObject 선택한 경우(프리팹 인스턴스 포함)
        var go = Selection.activeGameObject;
        if (go != null)
        {
            ScanGameObjectTree(go, go.name);
            return;
        }

        Debug.LogWarning("[MissingScriptFinder] Select a Prefab asset or a GameObject.");
    }

    private static void ScanPrefabAsset(GameObject prefabAsset)
    {
        var path = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[MissingScriptFinder] Invalid prefab path.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Debug.Log($"[MissingScriptFinder] Scanning Prefab Asset: {path}");
            ScanGameObjectTree(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ScanGameObjectTree(GameObject root, string context)
    {
        int found = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;

            // Missing script 컴포넌트는 GetComponents<Component>() 배열에서 null로 나옴
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    found++;
                    Debug.LogWarning(
                        $"[MissingScriptFinder] Missing Script at: {GetHierarchyPath(go)} (context: {context})",
                        go
                    );
                    EditorGUIUtility.PingObject(go);
                    // 한 번에 너무 많이 Ping 되는 게 싫으면 여기 주석처리해도 됨
                }
            }
        }

        if (found == 0)
            Debug.Log($"[MissingScriptFinder] No missing scripts found. (context: {context})");
        else
            Debug.Log($"[MissingScriptFinder] DONE. Found missing scripts: {found} (context: {context})");
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }
}
#endif
