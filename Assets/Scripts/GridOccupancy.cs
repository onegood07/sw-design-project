using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 다른 클래스에서도 사용하기 쉽게 public static으로 클래스 작성
public static class GridOccupancy
{
    // 타일맵 별로 어느 셀을 누가 갖고 있나 기록 (readonly는 읽기 전용)
    // key : Tilemap 객체 collisionTilemap
    // value: Dictionary<Vector2Int, Object> — 이 타일맵의 좌표(Vector2Int)별로 누가 그 셀을 차지하고 있는가 저장.
    private static readonly Dictionary<Tilemap, Dictionary<Vector2Int, Object>> map = new();

    // 주어진 타일맵 t에 대응되는 딕셔너리를 반환.
    private static Dictionary<Vector2Int, Object> Get(Tilemap t)
    {
        // 이미 좌표값이 존재하면 꺼내서 dict에 담고 없으면 새로 추가
        // out var은 딕셔너리에 값이 있으면 그 값을 꺼내고 없으면 null값을 꺼낸다
        if (!map.TryGetValue(t, out var dict))
        {
            dict = new Dictionary<Vector2Int, Object>();
            map[t] = dict;
        }
        return dict;
    }

    // 동일 셀에 다른 주인이 있으면 예약 실패
    // t : 타일맵의 좌표, cell : 셀 좌표 , 점유하는 주체
    public static bool TryReserve(Tilemap t, Vector2Int cell, Object who)
    {
        // t 타일맵 전용의 점유 딕셔너리를 가져오거나 없으면 새로 만들어서 반환
        var dict = Get(t);
        // 해당 셀(cell)에 이미 누가 있는지 찾아봄. 있으면 owner에 그 값을 넣고 true 반환, 없으면 false.
        // 그 셀에 이미 다른 누군가가 있다면 실패
        if (dict.TryGetValue(cell, out var owner) && owner != null && owner != who) return false;
        // 셀에 아무도 없다면 who가 점유
        dict[cell] = who;
        return true;
    }

    // 내가 점유 중일 때만 반납
    public static void Release(Tilemap t, Vector2Int cell, Object who)
    {
        // 현재 타일맵 t의 점유 딕셔너리를 가져옴.
        var dict = Get(t);
        // TryGetValue : 딕셔너리 안에서 해당 cell에 누가 있는지 찾음. 
        // owner == who 이면 그 셀의 주인이 지금 나 자신(who)일 때만 실행.
        // 즉, 남이 점유 중인 셀은 내가 마음대로 비우지 못하게 막음.
        if (dict.TryGetValue(cell, out var owner) && owner == who) dict.Remove(cell);
    }

    public static bool IsOccupied(Tilemap t, Vector2Int cell)
    {
        var dict = Get(t);
        // ContainsKey : 딕셔너리 안에 cell 키가 존재하면 true, 없으면 false.
        return dict.ContainsKey(cell);
    }
}
