using System.Collections.Generic;
using UnityEngine;

public class CorridorSocketProvider : MonoBehaviour
{
    [System.Serializable]
    public struct Socket
    {
        public Vector2Int cellOffset;      // 건물 pivot 셀 기준 오프셋
        public CorridorDirection direction; // 이 포트가 바깥으로 향하는 방향
    }

    [SerializeField] private Socket[] sockets;
    public IReadOnlyList<Socket> Sockets => sockets;
}
