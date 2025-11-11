using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class GridOccupancy
{
    // 타일맵 별로 "어느 셀을 누가 갖고 있나" 기록 (★ owner를 UnityEngine.Object로 일반화)
    private static readonly Dictionary<Tilemap, Dictionary<Vector2Int, Object>> map = new();

    private static Dictionary<Vector2Int, Object> Get(Tilemap t)
    {
        if (!map.TryGetValue(t, out var dict))
        {
            dict = new Dictionary<Vector2Int, Object>();
            map[t] = dict;
        }
        return dict;
    }

    // ★ 동일 셀에 다른 주인이 있으면 예약 실패
    public static bool TryReserve(Tilemap t, Vector2Int cell, Object who)
    {
        var dict = Get(t);
        if (dict.TryGetValue(cell, out var owner) && owner != null && owner != who) return false;
        dict[cell] = who;
        return true;
    }

    // ★ 내가 점유 중일 때만 반납
    public static void Release(Tilemap t, Vector2Int cell, Object who)
    {
        var dict = Get(t);
        if (dict.TryGetValue(cell, out var owner) && owner == who) dict.Remove(cell);
    }

    public static bool IsOccupied(Tilemap t, Vector2Int cell)
    {
        var dict = Get(t);
        return dict.ContainsKey(cell);
    }
}
