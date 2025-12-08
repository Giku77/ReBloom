using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DuplicateMeshCleaner
{
    [MenuItem("Tools/Cleanup/Remove Duplicate Meshes (Keep One)")]
    public static void RemoveDuplicateMeshes()
    {
        var root = Selection.activeTransform;
        if (root == null)
        {
            Debug.LogError("[DuplicateMeshCleaner] 씬에서 기준이 될 루트 오브젝트를 하나 선택하고 실행해줘.");
            return;
        }

        // (position, rotation, scale, mesh) 조합으로 중복 체크
        var map = new Dictionary<(Vector3, Quaternion, Vector3, Mesh), Transform>();

        int checkCount = 0;
        int removedCount = 0;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var t = mf.transform;
            var key = (t.position, t.rotation, t.lossyScale, mf.sharedMesh);
            checkCount++;

            if (map.ContainsKey(key))
            {
                // 이미 같은 위치/회전/스케일/메쉬가 있으면 이 놈은 중복 → 삭제
                Undo.DestroyObjectImmediate(t.gameObject);
                removedCount++;
            }
            else
            {
                map[key] = t;
            }
        }

        Debug.Log($"[DuplicateMeshCleaner] 검사 대상: {checkCount}개, 중복 제거: {removedCount}개");
    }
}
