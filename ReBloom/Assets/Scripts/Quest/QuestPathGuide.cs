using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class QuestPathGuide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] target;
    [SerializeField] private GameObject markerPrefab;

    public Transform[] Target => target;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float markerSpacing = 2f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private int maxMarkerCount = 50;

    private NavMeshPath path;
    private float timer;
    private int currentTargetIndex;
    private bool isActive;
    private readonly List<GameObject> markers = new();

    private void Awake()
    {
        path = new NavMeshPath();
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += UnbindLocalPlayer;
        TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= UnbindLocalPlayer;
    }

    public void SetTarget(Transform newTarget, int index = 0)
    {
        if (target == null || index < 0 || index >= target.Length || newTarget == null)
        {
            ClearTarget();
            return;
        }

        bool changed = !isActive || currentTargetIndex != index || target[index] != newTarget;

        currentTargetIndex = index;
        target[index] = newTarget;
        isActive = true;

        if (changed)
            ToastMessageUI.Instance?.Show("<color=#FFD93B>빛을 따라가세요!</color>");

        ForceUpdatePath();
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

    private void BindLocalPlayer(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        player = playerObject.transform;
        if (isActive)
            ForceUpdatePath();
    }

    private void UnbindLocalPlayer()
    {
        player = null;
        ClearMarkers();
    }

    private void TryBindExistingLocalPlayer()
    {
        var networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var networkObject in networkObjects)
        {
            if (!networkObject.IsOwner)
                continue;
            if (networkObject.GetComponent<PlayerController>() == null)
                continue;

            BindLocalPlayer(networkObject.gameObject);
            return;
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
                pos = hit.point + Vector3.up * groundOffset;
            else
                pos.y += groundOffset;

            markers[i].transform.position = pos;

            Vector3 dir;
            if (samplePoints.Count == 1)
                dir = target[currentTargetIndex].position - player.position;
            else if (i < samplePoints.Count - 1)
                dir = samplePoints[i + 1] - samplePoints[i];
            else
                dir = samplePoints[i] - samplePoints[i - 1];

            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                markers[i].transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            markers[i].SetActive(true);
        }
    }

    private List<Vector3> SamplePath(NavMeshPath navPath, float spacing)
    {
        List<Vector3> result = new();
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
        foreach (var marker in markers)
        {
            if (marker != null)
                marker.SetActive(false);
        }
    }
}
