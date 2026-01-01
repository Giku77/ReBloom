using System.Collections.Generic;
using UnityEngine;

public class FarmDroneOrbitController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform greenhouseRoot; // 흙들이 들어있는 부모(온실 루트)
    [SerializeField] private float hoverHeight = 2.2f;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float arriveDist = 0.2f;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 0.7f;
    [SerializeField] private float orbitSpeed = 180f; // degrees/sec
    [SerializeField] private float orbitDuration = 1.0f; // 각 포인트에서 빙글빙글 도는 시간

    private readonly List<Transform> _targets = new();
    private int _idx = 0;

    private float _orbitAngle;
    private float _orbitTimer;
    private float _nextRefreshTime;

    private void Start()
    {
        RefreshTargets();
        SnapToFirst();
    }

    private void Update()
    {
        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + 0.5f;
            RefreshTargets();
            if (_targets.Count == 0) return;
            _idx %= _targets.Count;
        }

        if (_targets.Count == 0) return;

        var target = _targets[_idx];
        if (target == null) { _idx = (_idx + 1) % _targets.Count; return; }

        // 목표 포인트의 topView 위치를 중심으로 원 궤도 오프셋 생성
        _orbitAngle += orbitSpeed * Time.deltaTime;
        var rad = _orbitAngle * Mathf.Deg2Rad;

        Vector3 center = target.position;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;

        Vector3 desired = center + orbitOffset;
        desired.y = center.y + hoverHeight;

        // 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, desired, moveSpeed * Time.deltaTime);

        Vector3 look = (center - transform.position);
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6f);

        // 충분히 돌았으면 다음 포인트로
        _orbitTimer += Time.deltaTime;
        if (_orbitTimer >= orbitDuration)
        {
            _orbitTimer = 0f;
            _idx = (_idx + 1) % _targets.Count;
        }
    }

    private void RefreshTargets()
    {
        _targets.Clear();
        if (greenhouseRoot == null) return;

        // 활성화된 흙들만 가져옴 (inactive 제외)
        var plots = greenhouseRoot.GetComponentsInChildren<FarmPlotTopView>(true);
        foreach (var p in plots)
        {
            if (p == null) continue;
            if (!p.gameObject.activeInHierarchy) continue;
            if (p.topView == null) continue;

            _targets.Add(p.topView);
        }

        // 항상 켜져있는 기본 흙 1개만 있어도 정상 동작
    }

    private void SnapToFirst()
    {
        if (_targets.Count == 0) return;
        var t = _targets[0];
        if (t == null) return;

        transform.position = t.position + Vector3.up * hoverHeight;
    }
}
