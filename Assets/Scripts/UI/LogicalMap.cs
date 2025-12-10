using System;
using System.Collections.Generic;
using UnityEngine;

public enum LogicalTileType
{
    Empty,
    Ground,
    Building,
    Blocking
}

[CreateAssetMenu(menuName = "Minimap/Logical Map")]
public class LogicalMap : ScriptableObject
{
    [Serializable]
    public struct CellData
    {
        public Vector2Int position;
        public LogicalTileType type;
    }

    public Vector2Int size;      // tilemap 크기
    public Vector2Int origin;    // tilemap 시작 좌표
    public List<CellData> cells = new();

    private Dictionary<Vector2Int, LogicalTileType> _cache;

    public LogicalTileType GetType(Vector2Int pos)
    {
        if (_cache == null)
        {
            _cache = new Dictionary<Vector2Int, LogicalTileType>();
            foreach (var c in cells)
                _cache[c.position] = c.type;
        }

        return _cache.TryGetValue(pos, out var t) ? t : LogicalTileType.Ground;
    }
}
