using System.Collections.Generic;
using UnityEngine;

public class TemporaryPlayerPassThrough : MonoBehaviour
{
    private readonly List<(Collider playerCol, Collider buildingCol)> _pairs = new();
    private bool _active;

    public void Init(Collider[] playerColliders, Collider[] buildingColliders)
    {
        foreach (var pc in playerColliders)
        {
            if (pc == null) continue;
            foreach (var bc in buildingColliders)
            {
                if (bc == null) continue;

                Physics.IgnoreCollision(pc, bc, true);
                _pairs.Add((pc, bc));
            }
        }

        _active = true;
    }

    private void Update()
    {
        if (!_active) return;

        bool stillOverlapping = false;

        // bounds 겹침 여부로 체크
        foreach (var (playerCol, buildingCol) in _pairs)
        {
            if (playerCol == null || buildingCol == null) 
                continue;

            if (playerCol.bounds.Intersects(buildingCol.bounds))
            {
                stillOverlapping = true;
                break;
            }
        }

        if (!stillOverlapping)
        {
            // 겹치는 거 없으면 다시 충돌 켜고 자기 삭제
            RestoreCollision();
            _active = false;
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (_active)
            RestoreCollision();
    }

    private void RestoreCollision()
    {
        foreach (var (playerCol, buildingCol) in _pairs)
        {
            if (playerCol == null || buildingCol == null) 
                continue;

            Physics.IgnoreCollision(playerCol, buildingCol, false);
        }
    }
}
