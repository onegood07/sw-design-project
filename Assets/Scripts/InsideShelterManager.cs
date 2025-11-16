using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("Hero Spawn Settings")]
    public Vector3 heroDefaultSpawn = new Vector3(0f, -3.0f, 0f);

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
        while (GameManager.Instance == null)
            yield return null;

        yield return new WaitForEndOfFrame();

        SpawnLeader();
        SpawnSurvivors();
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

        // 리더는 한 명 고정
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

            // 영웅 근처 제외
            if (Vector3.Distance(worldPos, heroDefaultSpawn) < minDistanceFromHero)
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
}
