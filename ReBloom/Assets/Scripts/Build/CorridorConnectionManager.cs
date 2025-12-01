using System;
using System.Collections.Generic;
using UnityEngine;

public class CorridorConnectionManager : MonoBehaviour
{
    public static CorridorConnectionManager I { get; private set; }

    private readonly Dictionary<Vector2Int, CorridorNode> nodes =
        new Dictionary<Vector2Int, CorridorNode>();

    private void Awake()
    {
        I = this;
    }

    public bool TryGetNodeAt(Vector2Int cell, out CorridorNode node)
    {
        return nodes.TryGetValue(cell, out node);
    }

    public void Register(CorridorNode node)
    {
        nodes[node.Cell] = node;
        ConnectNeighbors(node);
    }

    public void Unregister(CorridorNode node)
    {
        if (nodes.TryGetValue(node.Cell, out var n) && n == node)
        {
            nodes.Remove(node.Cell);
        }
        // 양쪽 neighbor 끊어주는 로직도 필요하면 추가
    }

    private void ConnectNeighbors(CorridorNode node)
    {
        // node의 회전값을 고려해서 "로컬 North/East/South/West" -> "월드 기준 방향" 변환해야 함
        // 우선 수평 회전만 있다고 가정 (y축 회전)

        var cell = node.Cell;
        float yRot = node.transform.eulerAngles.y;
        // 0, 90, 180, 270 기준으로 반올림
        int rotIndex = Mathf.RoundToInt(yRot / 90f) % 4;
        if (rotIndex < 0) rotIndex += 4;

        foreach (CorridorDirection localDir in Enum.GetValues(typeof(CorridorDirection)))
        {
            if (localDir == CorridorDirection.None) continue;
            if (!node.HasOpening(localDir)) continue;

            // 로컬 방향 -> 월드 기준 북동남서 방향으로 회전 적용
            CorridorDirection worldDir = RotateDirection(localDir, rotIndex);
            Vector2Int offset = DirectionToOffset(worldDir);
            Vector2Int neighborCell = cell + offset;

            if (!nodes.TryGetValue(neighborCell, out var neighbor))
                continue;

            // neighbor도 우리 쪽을 향해 열려있는지 확인
            var opposite = CorridorNode.Opposite(worldDir);
            if (!neighbor.HasOpening(RotateDirectionBack(opposite, neighbor.transform)))
                continue;

            // 연결
            node.SetNeighbor(worldDir, neighbor);
            neighbor.SetNeighbor(CorridorNode.Opposite(worldDir), node);
        }
    }

    public static Vector2Int DirectionToOffset(CorridorDirection dir)
    {
        return dir switch
        {
            CorridorDirection.North => new Vector2Int(0, 1),
            CorridorDirection.South => new Vector2Int(0, -1),
            CorridorDirection.East  => new Vector2Int(1, 0),
            CorridorDirection.West  => new Vector2Int(-1, 0),
            _                       => Vector2Int.zero
        };
    }

    public static CorridorDirection RotateDirection(CorridorDirection dir, int rotIndex)
    {
        // rotIndex: 0=0°, 1=90°, 2=180°, 3=270° (y축)
        for (int i = 0; i < rotIndex; i++)
        {
            dir = dir switch
            {
                CorridorDirection.North => CorridorDirection.East,
                CorridorDirection.East  => CorridorDirection.South,
                CorridorDirection.South => CorridorDirection.West,
                CorridorDirection.West  => CorridorDirection.North,
                _                       => dir
            };
        }
        return dir;
    }

    // neighbor가 어떤 회전인지는 transform 보고 다시 역변환하는 함수(대략 구조만)
    public static CorridorDirection RotateDirectionBack(CorridorDirection worldDir, Transform t)
    {
        float yRot = t.eulerAngles.y;
        int rotIndex = Mathf.RoundToInt(yRot / 90f) % 4;
        if (rotIndex < 0) rotIndex += 4;

        // worldDir을 -rotIndex 만큼 되돌리기
        for (int i = 0; i < rotIndex; i++)
        {
            worldDir = worldDir switch
            {
                CorridorDirection.North => CorridorDirection.West,
                CorridorDirection.West  => CorridorDirection.South,
                CorridorDirection.South => CorridorDirection.East,
                CorridorDirection.East  => CorridorDirection.North,
                _                       => worldDir
            };
        }
        return worldDir;
    }
}
