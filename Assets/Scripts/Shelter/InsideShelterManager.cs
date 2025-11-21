using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("Hero Spawn Settings")]
    public Tilemap spawnTilemap; // Hero 스폰용 타일맵
    public float heroMinDistance = 0.1f; // Hero 최소 스폰 거리
    public Vector3 outsidePosition = new Vector3(10.5f, 2.5f, 0f); // 쉘터 밖 위치 (쉘터 퇴장 시 설정할 좌표)

    [Header("NPC Prefab")]
    public GameObject survivorPrefab; // 생존자 NPC 프리팹
    public GameObject leaderPrefab; // 리더 NPC 프리팹

    [Header("Tilemaps")]
    public Tilemap groundTilemap; // 참고용 타일맵
    public Tilemap collisionTilemap; // 충돌 불가 타일맵 (벽, 장애물)
    public Tilemap leaderTilemap; // 리더 스폰 위치 타일맵
    public Tilemap survivorTilemap; // 생존자 스폰 위치 타일맵

    [Header("Spawn Settings")]
    public float minDistanceFromHero = 1.5f; // 생존자 스폰 시 Hero와 최소 거리

    // MARK: 시작 시 실행
    IEnumerator Start()
    {
        // GameManager 준비될 때까지 대기
        while (GameManager.Instance == null)
            yield return null;

        // 한 프레임 기다린 후 실행 (씬 로딩 완료 보장)
        yield return new WaitForEndOfFrame();

        // 영웅, 리더, 생존자 순서대로 스폰
        SpawnHero();
        SpawnLeader();
        SpawnSurvivors();
    }

    // MARK: 영웅 스폰 
    void SpawnHero()
    {
        if (spawnTilemap == null || HeroMoveControl.Instance == null) return;

        // 영웅 이동 컨트롤에 충돌 타일맵 전달
        HeroMoveControl.Instance.SetCollisionTilemap(collisionTilemap);

        Vector3 spawnPos = Vector3.zero;

        // spawnTilemap에서 스폰 가능한 타일 찾기
        foreach (var pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (!spawnTilemap.HasTile(pos)) continue;
            spawnPos = spawnTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0f); // 중앙 좌표로 변환
            break; // 첫 번째 타일 위치 사용
        }

        // 강제 위치 이동
        HeroMoveControl.Instance.ForceMove(spawnPos);
        Debug.Log($"Hero 위치 재설정 완료: {spawnPos}");
    }

    // MARK: 리더 NPC 스폰 
    void SpawnLeader()
    {
        if (leaderPrefab == null)
        {
            Debug.LogError("[InsideShelterManager] Leader Prefab이 없습니다!");
            return;
        }

        List<Vector3> leaderPositions = new List<Vector3>();
        BoundsInt bounds = leaderTilemap.cellBounds;

        // leaderTilemap 내 유효 위치 수집
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!leaderTilemap.HasTile(pos)) continue;        // 타일이 없으면 제외
            if (collisionTilemap.HasTile(pos)) continue;     // 충돌 타일이면 제외

            Vector3 worldPos = leaderTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0); // 중앙 좌표
            leaderPositions.Add(worldPos);
        }

        if (leaderPositions.Count == 0)
        {
            Debug.LogWarning("[InsideShelterManager] 리더 스폰 타일이 없습니다!");
            return;
        }

        // 첫 번째 위치에 리더 스폰
        Vector3 spawnPos = leaderPositions[0];
        Instantiate(leaderPrefab, spawnPos, Quaternion.identity);
    }

    // MARK: 생존자 NPC 스폰 
    void SpawnSurvivors()
    {
        if (survivorPrefab == null)
        {
            Debug.LogError("[InsideShelterManager] Survivor Prefab이 없습니다!");
            return;
        }

        int survivorCount = GameManager.Instance.SurvivorScore; // 스폰할 생존자 수
        
        List<Vector3> survivorPositions = new List<Vector3>();
        BoundsInt bounds = survivorTilemap.cellBounds;

        // survivorTilemap 내 유효 위치 수집
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!survivorTilemap.HasTile(pos)) continue;
            if (collisionTilemap.HasTile(pos)) continue;

            Vector3 worldPos = survivorTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

            // 영웅과 가까운 위치는 제외
            if (HeroMoveControl.Instance != null &&
                Vector3.Distance(worldPos, HeroMoveControl.Instance.transform.position) < minDistanceFromHero)
                continue;

            survivorPositions.Add(worldPos);
        }

        if (survivorPositions.Count == 0)
        {
            Debug.LogWarning("[InsideShelterManager] 생존자 스폰 가능한 타일이 없습니다!");
            return;
        }

        // 랜덤 위치로 생존자 스폰
        for (int i = 0; i < survivorCount; i++)
        {
            Vector3 spawnPos = survivorPositions[Random.Range(0, survivorPositions.Count)];
            Instantiate(survivorPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[InsideShelterManager] {survivorCount}명의 생존자를 스폰했습니다.");
    }

    // MARK: Hero가 쉘터 밖으로 이동했을 때
    public void OnHeroExitShelter()
    {
        if (HeroMoveControl.Instance != null)
        {
            HeroMoveControl.Instance.ExitShelter(outsidePosition); // 지정 위치로 이동
        }
    }
}
