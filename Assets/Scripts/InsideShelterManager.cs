using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("Hero Spawn Settings")]
    public Tilemap spawnTilemap;           
    public float heroMinDistance = 0.1f;   
     public Vector3 outsidePosition = new Vector3(10.5f, 2.5f, 0f);

    [Header("NPC Prefab")]
    public GameObject survivorPrefab;
    public GameObject leaderPrefab;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;     // 참고용
    public Tilemap collisionTilemap;  // 충돌 금지
    public Tilemap leaderTilemap;     // 리더 스폰 위치
    public Tilemap survivorTilemap;   // 생존자 스폰 위치

    [Header("Spawn Settings")]
    public float minDistanceFromHero = 1.5f;

    IEnumerator Start()
    {
        // GameManager 준비 대기
        while (GameManager.Instance == null)
            yield return null;

        yield return new WaitForEndOfFrame();

        SpawnHero();
        SpawnLeader();
        SpawnSurvivors();
    }

    void SpawnHero()
    {
        if (spawnTilemap == null || HeroMoveControl.Instance == null) return;

        HeroMoveControl.Instance.SetCollisionTilemap(collisionTilemap);

        Vector3 spawnPos = Vector3.zero;
        foreach (var pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (!spawnTilemap.HasTile(pos)) continue;
            spawnPos = spawnTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0f);
            break;
        }

        HeroMoveControl.Instance.ForceMove(spawnPos);
        Debug.Log($"Hero 위치 재설정 완료: {spawnPos}");
    }


    void SpawnLeader()
    {
        if (leaderPrefab == null)
        {
            Debug.LogError("Leader Prefab이 없습니다!");
            return;
        }

        List<Vector3> leaderPositions = new List<Vector3>();
        BoundsInt bounds = leaderTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!leaderTilemap.HasTile(pos)) continue;
            if (collisionTilemap.HasTile(pos)) continue;

            Vector3 worldPos = leaderTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);
            leaderPositions.Add(worldPos);
        }

        if (leaderPositions.Count == 0)
        {
            Debug.LogWarning("리더 스폰 타일이 없습니다!");
            return;
        }

        Vector3 spawnPos = leaderPositions[0];
        Instantiate(leaderPrefab, spawnPos, Quaternion.identity);
        Debug.Log("리더 소환 완료");
    }

    void SpawnSurvivors()
    {
        if (survivorPrefab == null)
        {
            Debug.LogError("Survivor Prefab이 없습니다!");
            return;
        }

        int survivorCount = GameManager.Instance.SurvivorScore;
        List<Vector3> survivorPositions = new List<Vector3>();

        BoundsInt bounds = survivorTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!survivorTilemap.HasTile(pos)) continue;
            if (collisionTilemap.HasTile(pos)) continue;

            Vector3 worldPos = survivorTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

            // Hero 근처 제외
            if (HeroMoveControl.Instance != null &&
                Vector3.Distance(worldPos, HeroMoveControl.Instance.transform.position) < minDistanceFromHero)
                continue;

            survivorPositions.Add(worldPos);
        }

        if (survivorPositions.Count == 0)
        {
            Debug.LogWarning("생존자 스폰 가능한 타일이 없습니다!");
            return;
        }

        for (int i = 0; i < survivorCount; i++)
        {
            Vector3 spawnPos = survivorPositions[Random.Range(0, survivorPositions.Count)];
            Instantiate(survivorPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"{survivorCount}명의 생존자를 스폰했습니다.");
    }

    public void OnHeroExitShelter()
    {
        if(HeroMoveControl.Instance != null)
        {
            HeroMoveControl.Instance.ExitShelter(outsidePosition);
        }
    }
}
