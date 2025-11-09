using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    [Header("Setting")]
    // TileMap 및 좀비 Prefab
    public Tilemap groundTilemap;  
    public Tilemap collisionTilemap;   
    public GameObject zombiePrefab;   

    // 소환할 좌표 
    private List<Vector3> spawnPositions = new List<Vector3>();
    // 소환한 Prefab
    private List<GameObject> spawnedZombies = new List<GameObject>();

    void Awake()
    {
        // 스폰 계산은 맨 처음에 한번만
        GetSpawnPositions();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

   // Tilemap에서 소환 가능 좌표 수집
    void GetSpawnPositions()
    {
        BoundsInt bounds = groundTilemap.cellBounds;
        TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds);
        
        // collisionTilemap null인지 확인하여 예외처리
        if (collisionTilemap == null)
        {
            Debug.LogError("[SpawnManager] GetSpawnPositions() - Collision Tilemap 할당 X");
            return;
        }

        spawnPositions.Clear();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                // 현재 셀 위치 계산
                Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);

                // groundTile에 타일이 있는지 확인
                TileBase groundTile = allGroundTiles[x + y * bounds.size.x];

                // collisionTilemap 확인
                TileBase collisionTile = collisionTilemap.GetTile(cellPos);
                
                // groundTile이 존재하며 collisionTile이 없을 때 (건물이 없는 곳)
                if (groundTile != null && collisionTile == null)
                {
                    // 월드 좌표로 변환
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
                    spawnPositions.Add(worldPos);
                }
            }
        }
    }

    // 기존 좀비 삭제
    public void ClearZombies()
    {
        foreach (var zombie in spawnedZombies)
        {
            if (zombie != null) Destroy(zombie);
        }

        spawnedZombies.Clear();
    }
    
    // 랜덤 좀비 소환
    public void SpawnZombies(int spawnCount)
    {
        // 기존 좀비 삭제
        ClearZombies();

        // 좌표 리스트를 새롭게 복제한 리스트
        List<Vector3> availablePositions = new List<Vector3>(spawnPositions);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePositions.Count == 0) break;

            // 랜덤으로 위치 선정
            int index = Random.Range(0, availablePositions.Count);
            Vector3 spawnPos = availablePositions[index];
            GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

            spawnedZombies.Add(zombie);

            availablePositions.RemoveAt(index);
        }
    }
}
