using System.Collections.Generic;
using UnityEngine;

public class CorridorSocketManager : MonoBehaviour
{
    public static CorridorSocketManager I { get; private set; }

    // (셀, 방향) -> 소켓 제공자(또는 건물)
    private readonly Dictionary<(Vector2Int, CorridorDirection), CorridorSocketProvider> map
        = new Dictionary<(Vector2Int, CorridorDirection), CorridorSocketProvider>();

    private void Awake()
    {
        I = this;
    }

    public void RegisterSockets(CorridorSocketProvider provider, Vector2Int baseCell, int rotIndex)
    {
        if (provider == null) return;

        foreach (var s in provider.Sockets)
        {
            // 오프셋은 회전 반영
            Vector2Int rotatedOffset = CorridorGrid.RotateOffset(s.cellOffset, rotIndex);
            Vector2Int cell = baseCell + rotatedOffset;

            // 방향도 회전 반영
            CorridorDirection worldDir = CorridorConnectionManager.RotateDirection(s.direction, rotIndex);

            map[(cell, worldDir)] = provider;
        }
    }

    public void UnregisterSockets(CorridorSocketProvider provider)
    {
        if (provider == null) return;

        // 간단 구현: 전체 스캔(개수 적으면 OK)
        // 최적화하려면 provider별로 등록키 목록을 저장해두는 방식 추천
        var keysToRemove = new List<(Vector2Int, CorridorDirection)>();
        foreach (var kv in map)
            if (kv.Value == provider) keysToRemove.Add(kv.Key);

        foreach (var k in keysToRemove)
            map.Remove(k);
    }

    public bool HasSocket(Vector2Int cell, CorridorDirection dir)
        => map.ContainsKey((cell, dir));
}
