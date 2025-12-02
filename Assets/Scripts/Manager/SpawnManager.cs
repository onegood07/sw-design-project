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
    public float baseRatio; // 스폰 확률 (아이템 타입별 스폰 확률 대비)
    [HideInInspector]
    public float ratio;
}
[System.Serializable]
public struct ItemSectorSpawnProbablity
{
    public ItemType type; // 아이템 타입
    [Range(0f,1f)]
    public float ratio; // 아이템 타입 스폰 확률
}
public class SpawnManager : MonoBehaviour
{
    // 싱글톤
    public static SpawnManager Instance { get; private set; }

    [Header("Tilemap")]
    public Tilemap groundTilemap;    // 바닥 타일맵
    public Tilemap collisionTilemap; // 장애물/충돌 타일맵

    [Header("아이템 종류별 스폰 확률 - 순서 변경 금지(ItemType 상의 순서로 고정)")]
    public ItemSectorSpawnProbablity[] ItemSectorInfos; // 아이템 종류별 스폰 정보
    [Header("Item Prefabs & 개별 아이템 스폰 확률(같은 아이템 종류 총합이 1을 넘지 않는 것을 권장)")]
    public ItemSpawnInfo[] itemInfos; // 개별 아이템 스폰 정보


    [Header("NPC Prefabs")]
    public GameObject[] npcPrefabs;     // NPC 프리팹 담을 리스트

    // MARK: - 좀비 프리팹을 타입별로 분리하여 관리
    [Header("Zombie Prefabs")]
    public GameObject normalZombiePrefab;   // Normal 좀비
    public GameObject highHpZombiePrefab;   // HighHp 좀비 
    public GameObject highSpeedZombiePrefab; // HighSpeed 좀비 
    public GameObject highPowerZombiePrefab; // HighPower 좀비 

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
        
        // [핵심]: 현재 대화 상태를 확인합니다.
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

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
            
            // [핵심]: 대화 중이라면, 새로 생성된 좀비에게 정지 상태를 바로 적용합니다. (모든 좀비 타입 처리)
            if (isDialogueActive)
            {
                // Grid 기반 좀비 (ZombieMove)
                var zombieMove = obj.GetComponent<ZombieMove>();
                if (zombieMove != null)
                {
                    zombieMove.isInDialogue = true;
                }
                
                // NavMesh 기반 좀비 (ZombieNavMove)
                var zombieNavMove = obj.GetComponent<ZombieNavMove>();
                if (zombieNavMove != null)
                {
                    zombieNavMove.isInDialogue = true;
                }
            }

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
        // 1-1. 전체 확률(Total Weight) 계산
        float totalWeight = 0f;
        foreach (var info in itemInfos)
        {
            totalWeight += info.ratio;
        }
        // 1-2. 총 아이템 수만큼 반복하며 룰렛 돌리기
        for (int i = 0; i < totalItemCount; i++)
        {
            if (remainingPositions.Count == 0) break; // 자리 없으면 중단

            float randomPoint = Random.value * totalWeight; // 0 ~ TotalWeight 사이 랜덤 값
            float currentWeightSum = 0f;
            int selectedIndex = -1;

            // 룰렛 확인: 어떤 아이템이 당첨됐나?
            for (int j = 0; j < itemInfos.Length; j++)
            {
                currentWeightSum += itemInfos[j].ratio;
                if (randomPoint <= currentWeightSum)
                {
                    selectedIndex = j;
                    break;
                }
            }

            // 당첨된 아이템 스폰
            if (selectedIndex != -1)
            {
                var selectedInfo = itemInfos[selectedIndex];
                
                // 위치 랜덤 선정 및 리스트에서 제거 (RemoveAt 사용)
                int posIndex = Random.Range(0, remainingPositions.Count);
                Vector3 spawnPos = remainingPositions[posIndex];
                remainingPositions.RemoveAt(posIndex);

                // 생성 및 리스트 추가
                GameObject obj = Instantiate(selectedInfo.prefab, spawnPos, Quaternion.identity);
                spawnedItems.Add(obj);

                if (itemManager != null)
                    itemManager.RegisterSpawnedItem(obj, selectedInfo.type);
            }
        }
        // foreach (var info in itemInfos)
        // {
        //     // info.ratio 를 확률로 계산해 두고 확률로 스폰 결정.
        //     for(int i = 0; i < totalItemCount; i++)
        //     {
        //         float randomRatio = Random.value; // 0.0 ~ 1.0 사이의 랜덤 실수
        //         for(int j = 0; i < itemInfos.Length; j++)
        //         {

        //         }
        //     }
        //     int count = Mathf.RoundToInt(totalItemCount * info.ratio); // 비율 계산
        //     List<Vector3> usedPositions = SpawnObjects(info.prefab, count, remainingPositions, spawnedItems, info.type);
        //     remainingPositions.RemoveAll(pos => usedPositions.Contains(pos)); // 사용 위치 제거
        // }

        // NPC 스폰
        int actualNpcCount = Mathf.Min(npcCount, npcPrefabs.Length); // NPC 종류 수보다 많이 스폰하지 않도록 제한

        if (npcPrefabs.Length == 0) // npc 프리팹이 없는 경우
        {
            Debug.LogWarning("[SpawnManager] 스폰할 NPC 프리팹이 지정되지 않았습니다.");
        }
        else
        {
            // 스폰할 NPC 프리팹을 랜덤하게 선택
            List<GameObject> npcsToSpawn = new List<GameObject>();
            
            // 프리팹 리스트 복사 후 셔플 (중복 방지하고 랜덤하게 선택)
            List<GameObject> availableNpcs = new List<GameObject>(npcPrefabs);
            
            // 원하는 NPC 수만큼 랜덤하게 선택
            for (int i = 0; i < actualNpcCount; i++)
            {
                if (availableNpcs.Count == 0) break;

                int randomIndex = Random.Range(0, availableNpcs.Count);
                npcsToSpawn.Add(availableNpcs[randomIndex]);
                availableNpcs.RemoveAt(randomIndex); // 이미 선택된 NPC는 제외
            }

            // 선택된 NPC들을 스폰
            foreach (GameObject npcPrefab in npcsToSpawn)
            {
                 // 1명씩 스폰하므로 count는 1
                List<Vector3> usedNpcPositions = SpawnObjects(npcPrefab, 1, remainingPositions, spawnedNPCs); 
                remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));
            }
        }

        // 좀비 스폰
        List<Vector3> usedZombiePositions = SpawnObjects(normalZombiePrefab, zombieCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedZombiePositions.Contains(pos));
    }

    // MARK: 좀비만 스폰 (밤 페이즈용)
    public void SpawnZombiesOnly(int zombieCount)
    {
        ClearZombies();

        if (allSpawnPositions.Count == 0)
        {
            GetSpawnPositions(); 
            if (allSpawnPositions.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다. 밤 좀비 스폰 생략.");
                return;
            }
        }

       // 아이템/NPC 위치를 피하기 위해 남은 위치를 계산
        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 기존 아이템/NPC 위치 근처 제외
        remainingPositions.RemoveAll(pos =>
            spawnedItems.Exists(item => item != null && Vector3.Distance(item.transform.position, pos) < 0.1f) ||
            spawnedNPCs.Exists(npc => npc != null && Vector3.Distance(npc.transform.position, pos) < 0.1f)
        );

        // 밤에는 특수 좀비 (HighHp, HighSpeed, HighPower)를 1/3씩 균등하게 분배
        int highHpCount = zombieCount / 3;
        int highSpeedCount = zombieCount / 3;
        int highPowerCount = zombieCount - highHpCount - highSpeedCount; 

        int totalSpawned = 0;

        // HighHp 좀비 스폰 (SpawnObjects 호출)
        List<Vector3> usedHpPositions = SpawnObjects(highHpZombiePrefab, highHpCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedHpPositions.Contains(pos));
        totalSpawned += usedHpPositions.Count;

        // HighSpeed 좀비 스폰 (SpawnObjects 호출)
        List<Vector3> usedSpeedPositions = SpawnObjects(highSpeedZombiePrefab, highSpeedCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedSpeedPositions.Contains(pos));
        totalSpawned += usedSpeedPositions.Count;

        // HighPower 좀비 스폰 (SpawnObjects 호출)
        List<Vector3> usedPowerPositions = SpawnObjects(highPowerZombiePrefab, highPowerCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedPowerPositions.Contains(pos));
        totalSpawned += usedPowerPositions.Count;

        // NOTE: 새로 스폰된 좀비들의 isInDialogue 관리는
        // SpawnObjects 내부에서 이미 처리되었으므로 추가적인 루프가 필요 없습니다.
        
        Debug.Log($"[SpawnManager] 밤 스폰 완료 - 좀비 총 {totalSpawned}마리 스폰 (HP: {usedHpPositions.Count}, Speed: {usedSpeedPositions.Count}, Power: {usedPowerPositions.Count})");
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

    // MARK: 아이템만 제거
    private void ClearItems()
    {
        foreach (var item in spawnedItems) 
            if (item != null) Destroy(item);
        spawnedItems.Clear();
    }

    // MARK: 좀비만 제거
    private void ClearZombies()
    {
        foreach (var zombie in spawnedZombies)
            if (zombie != null) Destroy(zombie);
        spawnedZombies.Clear();
    }

    // MARK: NPC만 제거
    private void ClearNPCs()
    {
        foreach (var npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();
    }
}