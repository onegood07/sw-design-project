using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    // Tilemap 세팅
    [Header("Tilemap")]
    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;

    // Prefab 세팅
    [Header("Prefabs")]
    public GameObject itemPrefab;
    public GameObject zombiePrefab;
    public GameObject npcPrefab;

    // 스폰 가능한 전체 좌표 저장
    private List<Vector3> allSpawnPositions = new List<Vector3>();

    // 스폰된 객체 저장
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();

    void Awake()
    {
        // 맨 처음에 스폰 가능 좌표 구하기
        GetSpawnPositions();
    }

    // 스폰 가능 좌표 구하기
    void GetSpawnPositions()
    {
        BoundsInt bounds = groundTilemap.cellBounds;
        TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds);

        if (collisionTilemap == null)
        {
            Debug.LogError("[SpawnManager] Collision Tilemap이 없습니다.");
            return;
        }

        allSpawnPositions.Clear();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);

                TileBase groundTile = allGroundTiles[x + y * bounds.size.x];
                TileBase collisionTile = collisionTilemap.GetTile(cellPos);

                // groundTile이 있고 충돌 타일이 없는 곳만 스폰 가능
                if (groundTile != null && collisionTile == null)
                {
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
                    allSpawnPositions.Add(worldPos);
                }
            }
        }
    }

    // 게임 오브젝트 소환하는 로직
    public List<Vector3> SpawnObjects(
        GameObject prefab,
        int count,
        List<Vector3> availablePositions,
        List<GameObject> outputList)
    {
        List<Vector3> usedPositions = new List<Vector3>();
        List<Vector3> copy = new List<Vector3>(availablePositions);

        for (int i = 0; i < count; i++)
        {
            if (copy.Count == 0) break;

            int index = Random.Range(0, copy.Count);
            Vector3 spawnPos = copy[index];

            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            outputList.Add(obj);

            usedPositions.Add(spawnPos);
            copy.RemoveAt(index);
        }

        return usedPositions;
    }

    // 스폰 로직
    public void StartSpawnProcess(int itemCount, int zombieCount, int npcCount)
    {
        // 전체 스폰 좌표 복사
        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 아이템 스폰
        List<Vector3> usedItemPositions =
            SpawnObjects(itemPrefab, itemCount, remainingPositions, spawnedItems);
        remainingPositions.RemoveAll(pos => usedItemPositions.Contains(pos));

         // NPC 스폰
        List<Vector3> usedNpcPositions =
            SpawnObjects(npcPrefab, npcCount, remainingPositions, spawnedNPCs);
        remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));

        // 좀비 스폰
        List<Vector3> usedZombiePositions =
            SpawnObjects(zombiePrefab, zombieCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedZombiePositions.Contains(pos));
    }

    // 전체 삭제
    public void ClearAll()
    {
        // 아이템 삭제
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item);
        spawnedItems.Clear();

        // 좀비 삭제
        foreach (var zombie in spawnedZombies)
            if (zombie != null) Destroy(zombie);
        spawnedZombies.Clear();

        // NPC 삭제
        foreach (var npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();
    }
}
