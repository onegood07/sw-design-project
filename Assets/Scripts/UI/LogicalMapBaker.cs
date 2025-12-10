using UnityEngine;
using UnityEngine.Tilemaps;

// 씬에 붙어서 "타일맵 + 충돌체 정보 → 논리 맵 데이터"를 굽는(생성하는) 컴포넌트
public class LogicalMapBaker : MonoBehaviour
{
    // 논리 맵을 만들 기준이 되는 타일맵 (땅 전체 범위 계산용)
    public Tilemap tilemap;

    // 건물 영역을 판별하기 위한 Collider 목록
    // (이 Collider 안에 있으면 Building 타일로 처리)
    public Collider2D[] buildingColliders;

    // 베이크 결과를 저장할 ScriptableObject (LogicalMap.asset)
    public LogicalMap output;

    // Inspector 우클릭 메뉴에서 실행할 수 있도록 등록
    // 컴포넌트 오른쪽 ⋮ → "Bake Logical Map" 클릭 가능
    [ContextMenu("Bake Logical Map")]
    public void Bake()
    {
        // 기존에 저장돼 있던 셀 데이터 전부 제거
        // (다시 베이크할 때 중복 저장 방지)
        output.cells.Clear();

        // 타일맵이 실제로 사용 중인 셀 영역(bounds)을 가져옴
        // 빈 타일은 자동으로 제외됨
        var bounds = tilemap.cellBounds;

        // 논리 맵의 기준 원점(좌하단 셀 좌표)
        output.origin = new Vector2Int(bounds.xMin, bounds.yMin);

        // 논리 맵의 전체 크기 (가로 x 세로)
        output.size = new Vector2Int(bounds.size.x, bounds.size.y);

        // 타일맵 영역 안의 모든 셀 좌표를 순회
        foreach (var cell in bounds.allPositionsWithin)
        {
            // 해당 셀의 월드 좌표 기준 "정중앙" 위치 계산
            // → Collider와의 충돌 판별에 사용
            Vector3 worldCenter = tilemap.GetCellCenterWorld(cell);

            // 기본 논리 타일 타입은 Ground(땅)으로 설정
            LogicalTileType type = LogicalTileType.Ground;

            // 모든 건물 Collider를 순회하며
            foreach (var col in buildingColliders)
            {
                // 셀 중앙 좌표가 어떤 건물 Collider 안에 포함되면
                if (col.OverlapPoint(worldCenter))
                {
                    // 해당 셀을 Building 타입으로 판정
                    type = LogicalTileType.Building;

                    // 이미 건물로 판정됐으니 더 검사할 필요 없음
                    break;
                }
            }

            // 계산된 결과를 논리 맵 셀 데이터로 저장
            output.cells.Add(new LogicalMap.CellData
            {
                // 타일 좌표 (Grid 기준 좌표)
                position = new Vector2Int(cell.x, cell.y),

                // 판정된 논리 타입 (Ground / Building 등)
                type = type
            });
        }

#if UNITY_EDITOR
        // 에디터에서 ScriptableObject가 변경되었음을 Unity에 알림
        // → 씬 저장 시 또는 재시작 후에도 데이터 유지
        UnityEditor.EditorUtility.SetDirty(output);
#endif

        // 콘솔에 베이크 완료 로그 출력
        Debug.Log("Logical Map Bake Complete");
    }
}
