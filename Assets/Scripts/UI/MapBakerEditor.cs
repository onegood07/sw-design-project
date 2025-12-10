#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;


// LogicalMapBaker 컴포넌트용 커스텀 인스펙터 선언
[CustomEditor(typeof(LogicalMapBaker))]

public class LogicalMapBakerEditor : Editor
{
    // Inspector가 그려질 때마다 호출됨
    public override void OnInspectorGUI()
    {
        // 기존 LogicalMapBaker의 public 필드들을 기본 Inspector 형태로 표시
        DrawDefaultInspector();

        // UI 간격 추가
        GUILayout.Space(10);

        // "Bake & Save LogicalMap PNG" 버튼 표시
        if (GUILayout.Button("Bake & Save LogicalMap PNG"))
        {
            // 버튼 클릭 시 베이크 + PNG 저장 실행
            BakeAndSave();
        }
    }

    // LogicalMap을 베이크하고 결과를 PNG로 저장하는 함수
    void BakeAndSave()
    {
        // 현재 Inspector에서 선택된 LogicalMapBaker 컴포넌트 가져오기
        var baker = (LogicalMapBaker)target;

        /* ========================= 
        (1) LogicalMap 베이크
        ========================= */

        // Tilemap + Collider 기준으로 LogicalMap 데이터 갱신
        baker.Bake();

        // baker 오브젝트가 수정되었음을 에디터에 알림 (씬 저장 가능 상태)
        EditorUtility.SetDirty(baker);

        Debug.Log("Logical Map baked.");

        /* ========================= 
        (2) 필수 참조 유효성 검사
        ========================= */

        // Output LogicalMap이나 Tilemap이 비어 있으면 중단
        if (baker.output == null || baker.tilemap == null)
        {
            Debug.LogError("Tilemap or LogicalMap output not assigned.");
            return;
        }

        // 타일맵이 실제로 사용하는 셀 범위 가져오기
        var bounds = baker.tilemap.cellBounds;

        // PNG로 만들 이미지의 가로/세로 크기
        int width = bounds.size.x;
        int height = bounds.size.y;

        // RGBA 포맷 텍스처 생성 (투명도 포함)
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // 픽셀 보간을 끄고 도트 스타일 유지
        tex.filterMode = FilterMode.Point;


        // 논리 타일 타입 → 색상 매핑 함수
        Color ColorOf(LogicalTileType type)
        {
            return type switch
            {
                // 건물 영역: 어두운 회색
                LogicalTileType.Building => new Color(0.15f, 0.15f, 0.15f, 1),

                // 장애물 타일: 파란색
                LogicalTileType.Blocking    => new Color(0.2f, 0.45f, 0.8f, 1),

                // 기본 땅: 초록색
                LogicalTileType.Ground   => new Color(0.25f, 0.6f, 0.25f, 1),

                // 정의되지 않은 타입: 투명
                _ => new Color(0, 0, 0, 0)
            };
        }

        // 타일맵 전체를 순회하며 픽셀 색상 지정
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // 타일 좌표 → 텍스처 좌표로 변환 (0부터 시작하도록 보정)
                int px = x - bounds.xMin;
                int py = y - bounds.yMin;

                // 해당 타일 좌표의 논리 타입 조회
                var type = baker.output.GetType(new Vector2Int(x, y));

                // 해당 위치 픽셀에 색상 설정
                tex.SetPixel(px, py, ColorOf(type));
            }
        }

        // SetPixel로 지정한 내용을 실제 텍스처에 적용
        tex.Apply();

        /* ========================= 
        (3) PNG 파일로 저장
        ========================= */

        // 저장할 폴더 경로
        string folder = "Assets/Minimap";

        // 폴더가 없으면 생성
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        // PNG 파일 전체 경로
        string path = folder + "/logical_map.png";

        // 텍스처를 PNG 포맷 바이트로 변환해 파일로 저장
        File.WriteAllBytes(path, tex.EncodeToPNG());

        /* ========================= 
        (4) Unity 에셋 Import 설정
        ========================= */

        // 새로 생성된 PNG를 Unity가 인식하도록 갱신
        AssetDatabase.Refresh();

        // 해당 PNG의 TextureImporter 가져오기
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            // 도트 유지 (보간 비활성화)
            importer.filterMode = FilterMode.Point;

            // 밉맵 비활성화 (미니맵에 불필요)
            importer.mipmapEnabled = false;

            // 압축 비활성화 (색상 왜곡 방지)
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // 런타임 읽기 불필요 → Read/Write 비활성화
            importer.isReadable = false;

            // 설정 적용 및 재임포트
            importer.SaveAndReimport();
        }

        Debug.Log("LogicalMap PNG saved to " + path);

        // Project 창에서 방금 생성한 PNG 에셋 자동 선택
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif