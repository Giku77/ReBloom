using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts/In Open Scenes")]
    public static void FindInOpenScenes()
    {
        int totalMissing = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            Debug.Log($"===== Scene: {scene.name} =====");

            foreach (var root in scene.GetRootGameObjects())
            {
                totalMissing += FindMissingInGameObjectRecursive(root, $"Scene/{scene.name}/{root.name}");
            }
        }

        Debug.Log($"[FindMissingScripts] 열린 씬 전체 missing script 개수: {totalMissing}");
    }

    [MenuItem("Tools/Find Missing Scripts/In Selected Object And Children")]
    public static void FindInSelected()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("선택된 GameObject가 없습니다.");
            return;
        }

        int totalMissing = FindMissingInGameObjectRecursive(
            Selection.activeGameObject,
            $"Selected/{Selection.activeGameObject.name}"
        );

        Debug.Log($"[FindMissingScripts] 선택 오브젝트 기준 missing script 개수: {totalMissing}");
    }

    [MenuItem("Tools/Find Missing Scripts/In All Prefabs")]
    public static void FindInAllPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int totalMissing = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            totalMissing += FindMissingInGameObjectRecursive(prefab, $"Prefab/{path}");
        }

        Debug.Log($"[FindMissingScripts] 전체 프리팹 missing script 개수: {totalMissing}");
    }

    private static int FindMissingInGameObjectRecursive(GameObject go, string path)
    {
        int missingCount = 0;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"[MissingScript] {path}", go);
                missingCount++;
            }
        }

        foreach (Transform child in go.transform)
        {
            missingCount += FindMissingInGameObjectRecursive(
                child.gameObject,
                $"{path}/{child.name}"
            );
        }

        return missingCount;
    }
}