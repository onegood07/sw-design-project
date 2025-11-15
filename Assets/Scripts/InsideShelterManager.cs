using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("NPC Prefab")]
    public GameObject survivorPrefab;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;      

    // [Header("Hero Spawn Settings")]
    // public Vector3 heroDefaultSpawn = new Vector3(0.5f, 0.5f, 0f); // Hero 초기 위치

//     IEnumerator Start()
//     {
//         if (GameManager.Instance == null)
//         {
//             Debug.LogError("씬에 GameManager가 존재하지 않습니다!");
//         }
//         else
//         {
//             Debug.Log("GameManager 발견! SurvivorScore: " + GameManager.Instance.SurvivorScore);
//         }

//         while (GameManager.Instance == null)
//             yield return null;

//         // 타일맵 자동 세팅
//         if (collisionTilemap == null)
//             collisionTilemap = GameObject.Find("collision")?.GetComponent<Tilemap>();

        

//         SpawnSurvivors();
//     }
    
//     void SpawnSurvivors()
//     {
//         List<Vector3> spawnPositions = GetSpawnPositions();

//         int survivorCount = GameManager.Instance.SurvivorScore;
//         int spawnCount = Mathf.Min(survivorCount, spawnPositions.Count);

//         for (int i = 0; i < spawnCount; i++)
//         {
//             int index = Random.Range(0, spawnPositions.Count);
//             Vector3 spawnPos = spawnPositions[index];
//             Instantiate(survivorPrefab, spawnPos, Quaternion.identity);
//             spawnPositions.RemoveAt(index);
//         }
//     }

//     List<Vector3> GetSpawnPositions()
//     {
//         List<Vector3> positions = new List<Vector3>();

//         if (groundTilemap == null || collisionTilemap == null)
//         {
//             Debug.LogError("Tilemap이 할당되지 않았습니다!");
//             return positions;
//         }

//         BoundsInt bounds = groundTilemap.cellBounds;
//         TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds);

//         for (int x = 0; x < bounds.size.x; x++)
//         {
//             for (int y = 0; y < bounds.size.y; y++)
//             {
//                 Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
//                 TileBase groundTile = allGroundTiles[x + y * bounds.size.x];
//                 TileBase collisionTile = collisionTilemap.GetTile(cellPos);

//                 if (groundTile != null && collisionTile == null)
//                 {
//                     Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
//                     positions.Add(worldPos);
//                 }
//             }
//         }

//         return positions;
//     }
}
