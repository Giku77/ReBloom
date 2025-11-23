using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class QuestPathGuide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] target;
    [SerializeField] private GameObject markerPrefab; // 빛나는 이정표 프리팹

    public Transform[] Target => target;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // 경로 재계산 주기
    [SerializeField] private float markerSpacing = 2f;    // 마커 간격
    [SerializeField] private float groundOffset = 0.05f;  // 지면에서 살짝 띄우기
    [SerializeField] private int maxMarkerCount = 50;     // 마커 최대 개수

    private NavMeshPath path;
    private float timer;

    private int currentTargetIndex = 0;
    private bool isActive;

    private readonly List<GameObject> markers = new List<GameObject>();

    private void Awake()
    {
        path = new NavMeshPath();
    }

    public void SetTarget(Transform newTarget, int index = 0)
    {
        currentTargetIndex = index;
        target[index] = newTarget;
        ToastMessageUI.Instance.Show("<color=#FFD93B>빛을 따라가세요!</color>");
        ForceUpdatePath();
        isActive = true;
    }

    public void ClearTarget()
    {
        isActive = false;
        ClearMarkers();
    }

    private void Update()
    {
        if (!isActive) return;

        if (player == null || target == null)
        {
            ClearMarkers();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdatePathAndMarkers();
        }
    }

    private void ForceUpdatePath()
    {
        timer = 0f;
        UpdatePathAndMarkers();
    }

    private void UpdatePathAndMarkers()
    {
        if (player == null || target == null || currentTargetIndex < 0 || currentTargetIndex >= target.Length || target[currentTargetIndex] == null)
        {
            Debug.LogWarning("[QuestPathGuide] 유효하지 않은 타겟/플레이어");
            ClearMarkers();
            return;
        }

        if (!NavMesh.SamplePosition(player.position, out NavMeshHit fromHit, 3f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[QuestPathGuide] 플레이어 주변에 NavMesh 없음: {player.position}");
            ClearMarkers();
            return;
        }

        if (!NavMesh.SamplePosition(target[currentTargetIndex].position, out NavMeshHit toHit, 3f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[QuestPathGuide] 타겟 주변에 NavMesh 없음: {target[currentTargetIndex].position}");
            ClearMarkers();
            return;
        }

        if (!NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path))
        {
            Debug.LogWarning("[QuestPathGuide] NavMesh.CalculatePath 실패");
            ClearMarkers();
            return;
        }

        if (path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"[QuestPathGuide] 경로 상태: {path.status}");
            ClearMarkers();
            return;
        }

        List<Vector3> samplePoints = SamplePath(path, markerSpacing);
        EnsureMarkerCount(samplePoints.Count);

        for (int i = 0; i < markers.Count; i++)
        {
            if (i >= samplePoints.Count)
            {
                markers[i].SetActive(false);
                continue;
            }

            Vector3 pos = samplePoints[i];

            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                pos = hit.point + Vector3.up * groundOffset;
            }
            else
            {
                pos.y += groundOffset;
            }

            markers[i].transform.position = pos;

            Vector3 dir;

            if (samplePoints.Count == 1)
            {
                dir = (target[currentTargetIndex].position - player.position);
            }
            else if (i < samplePoints.Count - 1)
            {
                dir = samplePoints[i + 1] - samplePoints[i];
            }
            else
            {
                dir = samplePoints[i] - samplePoints[i - 1];
            }

            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                // 파티클이 로컬 Z+ 방향으로 흐르도록 프리팹 세팅했다면
                // LookRotation(forward: dir)
                markers[i].transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            markers[i].SetActive(true);
        }
        // if (!NavMesh.CalculatePath(player.position, target[currentTargetIndex].position, NavMesh.AllAreas, path))
        // {
        //     Debug.LogWarning("[QuestPathGuide] 경로 계산 실패");
        //     ClearMarkers();
        //     return;
        // }

        // List<Vector3> samplePoints = SamplePath(path, markerSpacing);

        // EnsureMarkerCount(samplePoints.Count);

        // for (int i = 0; i < markers.Count; i++)
        // {
        //     if (i >= samplePoints.Count)
        //     {
        //         markers[i].SetActive(false);
        //         continue;
        //     }

        //     Vector3 pos = samplePoints[i];

        //     if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        //     {
        //         pos = hit.point + Vector3.up * groundOffset;
        //     }
        //     else
        //     {
        //         pos.y += groundOffset;
        //     }

        //     markers[i].transform.position = pos;
        //     markers[i].SetActive(true);
        // }
    }

    private List<Vector3> SamplePath(NavMeshPath navPath, float spacing)
    {
        List<Vector3> result = new List<Vector3>();
        if (navPath.corners.Length == 0)
            return result;

        Vector3 prev = navPath.corners[0];
        float distAccum = 0f;

        result.Add(prev);

        for (int i = 1; i < navPath.corners.Length; i++)
        {
            Vector3 curr = navPath.corners[i];
            Vector3 segment = curr - prev;
            float segmentLen = segment.magnitude;

            if (segmentLen <= 0.001f)
                continue;

            Vector3 dir = segment / segmentLen;

            while (distAccum + segmentLen >= spacing)
            {
                float remain = spacing - distAccum;
                prev += dir * remain;
                segmentLen -= remain;
                distAccum = 0f;

                result.Add(prev);
            }

            distAccum += segmentLen;
            prev = curr;
        }

        return result;
    }

    private void EnsureMarkerCount(int desiredCount)
    {
        desiredCount = Mathf.Min(desiredCount, maxMarkerCount);

        while (markers.Count < desiredCount)
        {
            var go = Instantiate(markerPrefab, transform);
            go.SetActive(false);
            markers.Add(go);
        }

        for (int i = 0; i < markers.Count; i++)
        {
            if (i >= desiredCount)
                markers[i].SetActive(false);
        }
    }

    private void ClearMarkers()
    {
        foreach (var m in markers)
        {
            if (m != null)
                m.SetActive(false);
        }
    }
}
