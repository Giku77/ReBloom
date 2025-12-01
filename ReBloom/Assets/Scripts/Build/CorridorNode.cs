using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum CorridorDirection
{
    None  = 0,
    North = 1 << 0,
    East  = 1 << 1,
    South = 1 << 2,
    West  = 1 << 3,
}

public class CorridorNode : MonoBehaviour
{
    [Header("Corridor Settings")]
    [SerializeField] private CorridorDirection openings; 
    public CorridorDirection Openings => openings;

    // 그리드 좌표
    public Vector2Int Cell { get; set; }

    // 연결된 이웃
    private readonly Dictionary<CorridorDirection, CorridorNode> neighbors =
        new Dictionary<CorridorDirection, CorridorNode>();

    public IReadOnlyDictionary<CorridorDirection, CorridorNode> Neighbors => neighbors;

    public void SetNeighbor(CorridorDirection dir, CorridorNode neighbor)
    {
        neighbors[dir] = neighbor;
    }

    public bool HasOpening(CorridorDirection dir)
    {
        return (openings & dir) != 0;
    }

    public static CorridorDirection Opposite(CorridorDirection dir)
    {
        return dir switch
        {
            CorridorDirection.North => CorridorDirection.South,
            CorridorDirection.South => CorridorDirection.North,
            CorridorDirection.East  => CorridorDirection.West,
            CorridorDirection.West  => CorridorDirection.East,
            _                       => CorridorDirection.None
        };
    }
}
