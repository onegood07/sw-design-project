using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 아이템 스폰 정보 구조체
[System.Serializable]
public struct ItemSpawnInfo
{
    public GameObject prefab; // 스폰할 아이템 프리팹
    public ItemType type; // 아이템 타입
    [Range(0f, 1f)]
    public float ratio; // 스폰 비율 (총 아이템 수 대비)
}

public class SpawnManager : MonoBehaviour
{
    // 싱글톤
    public static SpawnManager Instance { get; private set; }

    [Header("Tilemap")]
    public Tilemap groundTilemap;    // 바닥 타일맵
    public Tilemap collisionTilemap; // 장애물/충돌 타일맵

    [Header("Item Prefabs")]
    public ItemSpawnInfo[] itemInfos; // 아이템 종류별 정보

    [Header("Other Prefabs")]
    public GameObject zombiePrefab;   // 좀비 프리팹
    public GameObject npcPrefab;      // NPC 프리팹

    [Header("Managers")]
    public ItemManager itemManager;   // 아이템 등록 및 관리 매니저

    // 스폰 가능한 모든 위치
    private List<Vector3> allSpawnPositions = new List<Vector3>();

    // 현재 씬에 스폰된 오브젝트 리스트
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();

    void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
            GetSpawnPositions(); // 스폰 가능한 위치 초기화
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // MARK: 스폰 가능한 위치 계산 
    void GetSpawnPositions()
    {
        // tilemap이 할당되지 않은 경우
        if (groundTilemap == null || collisionTilemap == null)
        {
            Debug.LogWarning("[SpawnManager] Tilemap이 할당되지 않았습니다.");
            return;
        }

        BoundsInt bounds = groundTilemap.cellBounds; // 타일맵 전체 범위
        TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds); // 모든 타일 가져오기

        allSpawnPositions.Clear();

        // 타일맵 범위를 순회하며 스폰 가능 위치 확인
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
                TileBase groundTile = allGroundTiles[x + y * bounds.size.x];
                TileBase collisionTile = collisionTilemap.GetTile(cellPos);

                // 바닥 타일이 존재하고 충돌 타일이 없는 위치만 스폰 가능
                if (groundTile != null && collisionTile == null)
                {
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0); // 타일 중앙 좌표
                    allSpawnPositions.Add(worldPos);
                }
            }
        }
    }

    // MARK: 오브젝트 스폰 공용 함수
    private List<Vector3> SpawnObjects(GameObject prefab, int count, List<Vector3> availablePositions, List<GameObject> outputList, ItemType? type = null)
    {
        List<Vector3> usedPositions = new List<Vector3>();
        List<Vector3> copy = new List<Vector3>(availablePositions);

        for (int i = 0; i < count; i++)
        {
            if (copy.Count == 0) break; // 위치가 없으면 중단

            int index = Random.Range(0, copy.Count); // 랜덤 위치 선택
            Vector3 spawnPos = copy[index];
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity); // 오브젝트 생성

            outputList.Add(obj); // 스폰 리스트에 추가

            // 아이템일 경우 ItemManager에 등록
            if (type.HasValue && itemManager != null)
                itemManager.RegisterSpawnedItem(obj, type.Value);

            usedPositions.Add(spawnPos);
            copy.RemoveAt(index); // 이미 사용한 위치 제거
        }

        return usedPositions; // 사용된 위치 반환
    }

    // MARK: 아이템, NPC, 좀비 스폰 함수
    public void StartSpawnProcess(int totalItemCount, int npcCount, int zombieCount)
    {
        // 이미 스폰되어 있으면 재스폰하지 않음
        if (spawnedItems.Count > 0 || spawnedNPCs.Count > 0 || spawnedZombies.Count > 0)
            return;
        
        // 스폰 가능한 위치가 없는 경우
        if (allSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
            return;
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 아이템 종류별 비율 스폰
        foreach (var info in itemInfos)
        {
            int count = Mathf.RoundToInt(totalItemCount * info.ratio); // 비율 계산
            List<Vector3> usedPositions = SpawnObjects(info.prefab, count, remainingPositions, spawnedItems, info.type);
            remainingPositions.RemoveAll(pos => usedPositions.Contains(pos)); // 사용 위치 제거
        }

        // NPC 스폰
        List<Vector3> usedNpcPositions = SpawnObjects(npcPrefab, npcCount, remainingPositions, spawnedNPCs);
        remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));

        // 좀비 스폰
        List<Vector3> usedZombiePositions = SpawnObjects(zombiePrefab, zombieCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedZombiePositions.Contains(pos));
    }

    // MARK: 좀비만 스폰
    public void SpawnZombiesOnly(int zombieCount)
    {
        if (allSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
            return;
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 기존 아이템/NPC 위치 근처 제외
        remainingPositions.RemoveAll(pos =>
            spawnedItems.Exists(item => item != null && Vector3.Distance(item.transform.position, pos) < 0.1f) ||
            spawnedNPCs.Exists(npc => npc != null && Vector3.Distance(npc.transform.position, pos) < 0.1f)
        );

        SpawnObjects(zombiePrefab, zombieCount, remainingPositions, spawnedZombies);
    }

    // MARK: 모든 스폰 오브젝트 제거
    public void ClearAll()
    {
        foreach (var item in spawnedItems) 
            if (item != null) Destroy(item);
        spawnedItems.Clear();
        itemManager?.ClearItems(); // ItemManager에서도 제거

        foreach (var zombie in spawnedZombies)
            if (zombie != null) Destroy(zombie);
        spawnedZombies.Clear();

        foreach (var npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();
    }
}
