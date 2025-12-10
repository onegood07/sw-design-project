using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform minimapPanel;
    public RectTransform mapImage;
    public RectTransform playerIcon;
    public Transform playerWorld;


    [Header("Map Source")]
    public Tilemap tilemap;


    [Header("Map Settings")]
    public Vector2 worldMin;
    public Vector2 worldMax;

    Vector2 minimapSize;
    Vector2 mapSize;

    void Start()
    {
        // Tilemap이 있으면 Bounds를 읽어서 worldMin/worldMax 자동 설정
        if (tilemap != null)
        {
            BoundsInt bounds = tilemap.cellBounds;

            Vector3 min = tilemap.CellToWorld(bounds.min);
            Vector3 max = tilemap.CellToWorld(bounds.max);

            // 맵 전체 크기
            Vector2 mapWorldSize = new Vector2(
                max.x - min.x,
                max.y - min.y
            );

            // 플레이어 시작 위치 = 맵 중심
            Vector2 center = playerWorld.position;

            worldMin = new Vector2(min.x, min.y);
            worldMax = new Vector2(max.x, max.y);
        }

        RectTransform viewport = mapImage.parent as RectTransform;
        minimapSize = ((RectTransform)mapImage.parent).rect.size;
        mapSize = mapImage.rect.size;

        // 시작 시 중앙 정렬
        mapImage.anchoredPosition = Vector2.zero;

        // 디버깅
        Debug.Log($"worldMin: {worldMin}");
        Debug.Log($"worldMax: {worldMax}");
        Debug.Log($"worldCenter: {(worldMin + worldMax) * 0.5f}");
        Debug.Log($"playerPos: {playerWorld.position}");
    }

    void Update()
    {
        UpdateMapPosition();
    }

    void UpdateMapPosition()
    {
        // 1. 플레이어 월드 → 0~1 정규화
        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, playerWorld.position.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, playerWorld.position.y);
        Debug.Log($"nx:{nx}, ny:{ny}");


        // 2. 맵 좌표로 변환
        float mapX = nx * mapSize.x;
        float mapY = ny * mapSize.y;

        // 3. 플레이어 중앙 고정 → 맵 반대 이동
        Vector2 targetPos = new Vector2(
            -mapX + minimapSize.x * 0.5f,
            -mapY + minimapSize.y * 0.5f
        );

        // 4. 클램핑
        targetPos = ClampMapPosition(targetPos);

        mapImage.anchoredPosition = targetPos;
    }

    Vector2 ClampMapPosition(Vector2 pos)
    {
        float clampX = Mathf.Clamp(
            pos.x,
            minimapSize.x - mapSize.x,
            0
        );

        float clampY = Mathf.Clamp(
            pos.y,
            minimapSize.y - mapSize.y,
            0
        );

        return new Vector2(clampX, clampY);
    }
}
