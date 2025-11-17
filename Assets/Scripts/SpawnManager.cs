using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public struct ItemSpawnInfo
{
    public GameObject prefab;
    public ItemType type;
    [Range(0f, 1f)]
    public float ratio;
}

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Tilemap")]
    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;

    [Header("Item Prefabs")]
    public ItemSpawnInfo[] itemInfos;

    [Header("Other Prefabs")]
    public GameObject zombiePrefab;
    public GameObject npcPrefab;

    [Header("Managers")]
    public ItemManager itemManager;

    private List<Vector3> allSpawnPositions = new List<Vector3>();

    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();

    void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GetSpawnPositions();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void GetSpawnPositions()
    {
        if (groundTilemap == null || collisionTilemap == null)
        {
            Debug.LogWarning("[SpawnManager] Tilemap이 할당되지 않았습니다.");
            return;
        }

        BoundsInt bounds = groundTilemap.cellBounds;
        TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds);

        allSpawnPositions.Clear();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
                TileBase groundTile = allGroundTiles[x + y * bounds.size.x];
                TileBase collisionTile = collisionTilemap.GetTile(cellPos);

                if (groundTile != null && collisionTile == null)
                {
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
                    allSpawnPositions.Add(worldPos);
                }
            }
        }
    }

    private List<Vector3> SpawnObjects(GameObject prefab, int count, List<Vector3> availablePositions, List<GameObject> outputList, ItemType? type = null)
    {
        List<Vector3> usedPositions = new List<Vector3>();
        List<Vector3> copy = new List<Vector3>(availablePositions);

        for (int i = 0; i < count; i++)
        {
            if (copy.Count == 0) break;

            int index = Random.Range(0, copy.Count);
            Vector3 spawnPos = copy[index];
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 씬 전환에도 유지 (오브젝트들)
            DontDestroyOnLoad(obj);

            outputList.Add(obj);

            if (type.HasValue && itemManager != null)
                itemManager.RegisterSpawnedItem(obj, type.Value);

            usedPositions.Add(spawnPos);
            copy.RemoveAt(index);
        }

        return usedPositions;
    }

    public void StartSpawnProcess(int totalItemCount, int npcCount, int zombieCount)
    {
        // 이미 스폰되어 있으면 재스폰하지 않음
        if (spawnedItems.Count > 0 || spawnedNPCs.Count > 0 || spawnedZombies.Count > 0)
            return;

        if (allSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
            return;
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 아이템 종류별 비율 스폰
        foreach (var info in itemInfos)
        {
            int count = Mathf.RoundToInt(totalItemCount * info.ratio);
            List<Vector3> usedPositions = SpawnObjects(info.prefab, count, remainingPositions, spawnedItems, info.type);
            remainingPositions.RemoveAll(pos => usedPositions.Contains(pos));
        }

        // NPC 스폰
        List<Vector3> usedNpcPositions = SpawnObjects(npcPrefab, npcCount, remainingPositions, spawnedNPCs);
        remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));

        // 좀비 스폰
        List<Vector3> usedZombiePositions = SpawnObjects(zombiePrefab, zombieCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedZombiePositions.Contains(pos));
    }

    public void SpawnZombiesOnly(int zombieCount)
    {
        if (allSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
            return;
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        remainingPositions.RemoveAll(pos =>
            spawnedItems.Exists(item => item != null && Vector3.Distance(item.transform.position, pos) < 0.1f) ||
            spawnedNPCs.Exists(npc => npc != null && Vector3.Distance(npc.transform.position, pos) < 0.1f)
        );

        SpawnObjects(zombiePrefab, zombieCount, remainingPositions, spawnedZombies);
    }

    public void ClearAll()
    {
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item);
        spawnedItems.Clear();
        itemManager?.ClearItems();

        foreach (var zombie in spawnedZombies)
            if (zombie != null) Destroy(zombie);
        spawnedZombies.Clear();

        foreach (var npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();
    }
}
