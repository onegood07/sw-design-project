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

    // [Header("Tilemaps")]
    // public Tilemap groundTilemap;
    // // public Tilemap collisionTilemap;

    // IEnumerator Start()
    // {
    //     if (GameManager.Instance == null)
    //     {
    //         Debug.LogError("씬에 GameManager가 존재하지 않습니다!");
    //     }
        
    //     while (GameManager.Instance == null)
    //         yield return null;

    //     Debug.Log("GameManager 발견! SurvivorScore: " + GameManager.Instance.SurvivorScore);

    //     AdjustHeroPosition();
        
    //     // if (collisionTilemap == null)
    //     // {
    //     //     GameObject collisionObj = GameObject.Find("collision");
    //     //     if (collisionObj != null)
    //     //         collisionTilemap = collisionObj.GetComponent<Tilemap>();
    //     // }

    //     // SpawnSurvivors();
    // }
    
    // // Hero 위치 조정 함수
    // void AdjustHeroPosition()
    // {
    //     HeroMoveControl heroController = HeroMoveControl.Instance;

    //     if (heroController != null)
    //     {
    //         heroController.transform.position = heroDefaultSpawn;
    //         Debug.Log($"Hero의 위치를 쉘터 시작 지점 ({heroDefaultSpawn})로 설정했습니다.");
    //     }
    //     else
    //     {
    //         Debug.LogError("HeroMoveControl 인스턴스를 찾을 수 없습니다!");
    //     }
    // }
    
    // // 기존 Survivor 스폰 로직 (주석 해제)
    // void SpawnSurvivors()
    // {
    //     List<Vector3> spawnPositions = GetSpawnPositions();

    //     int survivorCount = GameManager.Instance.SurvivorScore;
    //     int spawnCount = Mathf.Min(survivorCount, spawnPositions.Count);

    //     for (int i = 0; i < spawnCount; i++)
    //     {
    //         int index = Random.Range(0, spawnPositions.Count);
    //         Vector3 spawnPos = spawnPositions[index];
    //         Instantiate(survivorPrefab, spawnPos, Quaternion.identity);
    //         spawnPositions.RemoveAt(index);
    //     }
    // }

    // List<Vector3> GetSpawnPositions()
    // {
    //     List<Vector3> positions = new List<Vector3>();

    //     if (groundTilemap == null || collisionTilemap == null)
    //     {
    //         Debug.LogError("Tilemap이 할당되지 않았습니다!");
    //         return positions;
    //     }

    //     BoundsInt bounds = groundTilemap.cellBounds;
    //     TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds);

    //     for (int x = 0; x < bounds.size.x; x++)
    //     {
    //         for (int y = 0; y < bounds.size.y; y++)
    //         {
    //             Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
    //             TileBase groundTile = allGroundTiles[x + y * bounds.size.x];
    //             TileBase collisionTile = collisionTilemap.GetTile(cellPos);

    //             if (groundTile != null && collisionTile == null)
    //             {
    //                 Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
    //                 positions.Add(worldPos);
    //             }
    //         }
    //     }

    //     return positions;
    // }
}