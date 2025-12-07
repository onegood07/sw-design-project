using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // List.Sum, List.FindIndex 등을 사용하기 위해 추가


// 아이템 스폰 정보 구조체 (기존 구조체)
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

// 🌟 [추가] 씬 전환 시 유지할 아이템 정보 구조체
[System.Serializable]
public struct PersistentItemData
{
    public Vector3 position;
    public string prefabName; // 프리팹을 찾기 위한 이름 (복원 시 사용)
    public ItemType type;
}

// 🌟 [추가] 씬 전환 시 유지할 NPC 정보 구조체
[System.Serializable]
public struct PersistentNPCData
{
    public Vector3 position;
    public string prefabName; // 프리팹을 찾기 위한 이름 (복원 시 사용)
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

    // 🌟 씬 로드 시 파괴되지 않는 영구 데이터 리스트
    private List<PersistentItemData> persistentItems = new List<PersistentItemData>();
    private List<PersistentNPCData> persistentNPCs = new List<PersistentNPCData>();

    // 현재 씬에 스폰된 오브젝트 리스트 (복원 후 참조 관리용)
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            GetSpawnPositions(); // 초기 스폰 위치 계산
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // 🌟 씬이 로드될 때 호출되는 이벤트 등록
    void OnEnable()
    {
        // SceneManager를 사용하기 위해 UnityEngine.SceneManagement이 필요합니다.
        // 현재 코드 상단에 using UnityEngine.SceneManagement;이 없으므로 명시적으로 지정합니다.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // GameManager 인스턴스가 준비되었는지 확인
        if (GameManager.Instance == null) return;
        
        // 🌟 메인 월드 씬으로 돌아왔을 때 복원 로직 실행
        if (scene.name == GameManager.Instance.MainWorldSceneName) 
        {
            // 새 씬의 타일맵 정보를 다시 가져옵니다.
            GetSpawnPositions(); 
            
            if (persistentItems.Count > 0 || persistentNPCs.Count > 0)
            {
                // 기존 데이터가 있다면 복원
                RestorePersistentObjects();
            }
            // 기존 데이터가 없다면 (첫 스폰이 필요하다면) GameManager에서 StartSpawnProcess를 호출해야 합니다.
        }
        // 🌟 쉘터 씬으로 이동 시, 현재 씬의 모든 스폰된 오브젝트를 클리어합니다.
        else if (scene.name == GameManager.Instance.ShelterSceneName)
        {
            ClearAll(); 
        }
    }


    // MARK: - 아이템 섹터 비율 적용 함수
    public void ApplyItemSectorRatios(float heal, float weapon, float lantern, float quest, float material, float submit)
    {
        if (ItemSectorInfos.Length >= 6)
        {
            // 1. 섹터별 비율 업데이트
            ItemSectorInfos[0].ratio = heal; 
            ItemSectorInfos[1].ratio = weapon; 
            ItemSectorInfos[2].ratio = lantern; 
            ItemSectorInfos[3].ratio = quest; 
            ItemSectorInfos[4].ratio = material; 
            ItemSectorInfos[5].ratio = submit; 

            Debug.Log($"[SpawnManager] 신규 스폰 섹터 비율 적용 완료. Submit 비율: {submit:P2}");

            // 2. 개별 아이템 비율 업데이트 (itemInfos)
            if (itemInfos != null)
            {
                for(int i = 0; i < itemInfos.Length; i++)
                {
                    ItemSpawnInfo itemInfo = itemInfos[i];
                    float sectorRatio = 0f;

                    switch (itemInfo.type)
                    {
                        case ItemType.Heal: sectorRatio = heal; break;
                        case ItemType.Weapon: sectorRatio = weapon; break;
                        case ItemType.Lantern: sectorRatio = lantern; break;
                        case ItemType.Quest: sectorRatio = quest; break;
                        case ItemType.Material: sectorRatio = material; break;
                        case ItemType.Submit: sectorRatio = submit; break;
                    }

                    ItemSpawnInfo tempInfo = itemInfos[i];
                    tempInfo.ratio = sectorRatio * itemInfo.baseRatio;
                    itemInfos[i] = tempInfo;
                }
            }
        }
    }


    // MARK: 스폰 가능한 위치 계산 
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
                // GetTilesBlock 배열은 1차원 배열로 평탄화되어 있으므로 인덱스 계산 필요
                int index = x + y * bounds.size.x; 
                if (index >= allGroundTiles.Length) continue;

                TileBase groundTile = allGroundTiles[index];
                TileBase collisionTile = collisionTilemap.GetTile(cellPos);

                if (groundTile != null && collisionTile == null)
                {
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0); 
                    allSpawnPositions.Add(worldPos);
                }
            }
        }
    }

    // MARK: 오브젝트 스폰 공용 함수 (🌟 [수정] 지속 데이터 기록 로직 추가)
  // SpawnManager.cs

private List<Vector3> SpawnObjects(GameObject prefab, int count, List<Vector3> availablePositions, List<GameObject> outputList, ItemType? type = null)
{
    List<Vector3> usedPositions = new List<Vector3>();
    List<Vector3> copy = new List<Vector3>(availablePositions);
    
    // ❌ isDialogueActive 변수를 사용하여 좀비를 수동으로 잠글 필요가 없어졌습니다.
    // bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive; 
    
    // Item/NPC 프리팹 이름 가져오기
    string prefabName = prefab.name;

    for (int i = 0; i < count; i++)
    {
        if (copy.Count == 0) break; 

        int index = Random.Range(0, copy.Count); 
        Vector3 spawnPos = copy[index];
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity); 

        outputList.Add(obj); 

        // 아이템일 경우 ItemManager에 등록 및 Persistent Item Data 기록
        if (type.HasValue && itemManager != null)
        {
            itemManager.RegisterSpawnedItem(obj, type.Value);
            
            // 🌟 Persistent Item Data 기록
            persistentItems.Add(new PersistentItemData
            {
                position = spawnPos,
                prefabName = prefabName,
                type = type.Value
            });
        }
        // NPC일 경우 Persistent NPC Data 기록 (type이 null이면서 NPC 프리팹인 경우)
        else if (type == null && outputList == spawnedNPCs)
        {
            // 🌟 Persistent NPC Data 기록
            persistentNPCs.Add(new PersistentNPCData
            {
                position = spawnPos,
                prefabName = prefabName
            });
        }
        
        // ❌ 좀비 정지 로직 제거 (이제 GameManager 상태에 따라 좀비 스스로 정지합니다.)
        /*
        if (isDialogueActive)
        {
            var zombieMove = obj.GetComponent<ZombieMove>(); 
            if (zombieMove != null) { zombieMove.isInDialogue = true; }
            
            var zombieNavMove = obj.GetComponent<ZombieNavMove>(); 
            if (zombieNavMove != null) { zombieNavMove.isInDialogue = true; }
        }
        */

        usedPositions.Add(spawnPos);
        copy.RemoveAt(index); 
    }

    return usedPositions; 
}
    // MARK: - 🌟 [추가] 지속 오브젝트 복원 함수
    public void RestorePersistentObjects()
    {
        // 씬에서 이미 스폰된 오브젝트 목록 초기화
        ClearItems();
        ClearNPCs();
        
        // 1. 아이템 복원
        foreach (var itemData in persistentItems)
        {
            GameObject prefabToSpawn = GetItemPrefabByName(itemData.prefabName);
            
            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, itemData.position, Quaternion.identity);
                spawnedItems.Add(obj);
                itemManager?.RegisterSpawnedItem(obj, itemData.type);
            }
        }
        
        // 2. NPC 복원
        foreach (var npcData in persistentNPCs)
        {
            GameObject prefabToSpawn = GetNpcPrefabByName(npcData.prefabName);

            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, npcData.position, Quaternion.identity);
                spawnedNPCs.Add(obj);
            }
        }
        
        Debug.Log($"[SpawnManager] 오브젝트 복원 완료: 아이템 {spawnedItems.Count}개, NPC {spawnedNPCs.Count}명");
    }

    // 🌟 프리팹 이름으로 아이템 프리팹을 찾는 헬퍼 함수
    private GameObject GetItemPrefabByName(string name)
    {
        foreach (var info in itemInfos)
        {
            if (info.prefab != null && info.prefab.name == name)
            {
                return info.prefab;
            }
        }
        return null;
    }

    // 🌟 프리팹 이름으로 NPC 프리팹을 찾는 헬퍼 함수
    private GameObject GetNpcPrefabByName(string name)
    {
        foreach (var prefab in npcPrefabs)
        {
            if (prefab != null && prefab.name == name)
            {
                return prefab;
            }
        }
        return null;
    }


    // MARK: 아이템, NPC, 좀비 스폰 함수 (🌟 [수정] 영구 데이터가 없을 때만 실행)
    public void StartSpawnProcess(int totalItemCount, int npcCount, int zombieCount)
    {
        // 이미 영구 데이터가 있다면 초기 스폰을 건너뜁니다.
        if (persistentItems.Count > 0 || persistentNPCs.Count > 0)
        {
            Debug.LogWarning("[SpawnManager] 영구 데이터가 존재하여 초기 스폰을 건너뛰었습니다. 복원 로직을 사용해야 합니다.");
            return;
        }

        if (spawnedItems.Count > 0 || spawnedNPCs.Count > 0 || spawnedZombies.Count > 0)
            return;
        
        if (allSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
            return;
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);
        
        // 1. 아이템 스폰 및 Persistent Item Data 기록
        float totalWeight = itemInfos.Sum(info => info.ratio);

        for (int i = 0; i < totalItemCount; i++)
        {
            if (remainingPositions.Count == 0 || totalWeight <= 0f) break; 

            float randomPoint = Random.value * totalWeight; 
            float currentWeightSum = 0f;
            int selectedIndex = -1;

            for (int j = 0; j < itemInfos.Length; j++)
            {
                currentWeightSum += itemInfos[j].ratio;
                if (randomPoint <= currentWeightSum)
                {
                    selectedIndex = j;
                    break;
                }
            }

            if (selectedIndex != -1)
            {
                var selectedInfo = itemInfos[selectedIndex];
                
                int posIndex = Random.Range(0, remainingPositions.Count);
                Vector3 spawnPos = remainingPositions[posIndex];
                remainingPositions.RemoveAt(posIndex);

                GameObject obj = Instantiate(selectedInfo.prefab, spawnPos, Quaternion.identity);
                spawnedItems.Add(obj);

                if (itemManager != null)
                    itemManager.RegisterSpawnedItem(obj, selectedInfo.type);
                    
                // 🌟 Persistent Item Data 기록
                persistentItems.Add(new PersistentItemData
                {
                    position = spawnPos,
                    prefabName = selectedInfo.prefab.name,
                    type = selectedInfo.type
                });
            }
        }

        // 2. NPC 스폰 및 Persistent NPC Data 기록
        int actualNpcCount = Mathf.Min(npcCount, npcPrefabs.Length); 

        if (npcPrefabs.Length == 0) 
        {
            Debug.LogWarning("[SpawnManager] 스폰할 NPC 프리팹이 지정되지 않았습니다.");
        }
        else
        {
            List<GameObject> npcsToSpawn = new List<GameObject>();
            List<GameObject> availableNpcs = new List<GameObject>(npcPrefabs);
            
            for (int i = 0; i < actualNpcCount; i++)
            {
                if (availableNpcs.Count == 0) break;

                int randomIndex = Random.Range(0, availableNpcs.Count);
                npcsToSpawn.Add(availableNpcs[randomIndex]);
                availableNpcs.RemoveAt(randomIndex); 
            }

            foreach (GameObject npcPrefab in npcsToSpawn)
            {
                // SpawnObjects를 사용하여 생성 및 persistentNPCs에 기록
                List<Vector3> usedNpcPositions = SpawnObjects(npcPrefab, 1, remainingPositions, spawnedNPCs); 
                remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));
                
                // SpawnObjects 내부에서 Persistent NPC Data 기록 로직이 처리됨.
            }
        }

        // 3. 좀비 스폰 (Persistent Data 기록 없음)
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

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 아이템/NPC 위치를 피하기 위해 남은 위치를 계산
        // 🌟 복원될 아이템/NPC의 위치를 참조하여 피합니다.
        remainingPositions.RemoveAll(pos =>
            persistentItems.Exists(item => Vector3.Distance(item.position, pos) < 0.1f) ||
            persistentNPCs.Exists(npc => Vector3.Distance(npc.position, pos) < 0.1f)
        );

        // 밤에는 특수 좀비 (HighHp, HighSpeed, HighPower)를 1/3씩 균등하게 분배
        int highHpCount = zombieCount / 3;
        int highSpeedCount = zombieCount / 3;
        int highPowerCount = zombieCount - highHpCount - highSpeedCount; 

        int totalSpawned = 0;

        List<Vector3> usedHpPositions = SpawnObjects(highHpZombiePrefab, highHpCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedHpPositions.Contains(pos));
        totalSpawned += usedHpPositions.Count;

        List<Vector3> usedSpeedPositions = SpawnObjects(highSpeedZombiePrefab, highSpeedCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedSpeedPositions.Contains(pos));
        totalSpawned += usedSpeedPositions.Count;

        List<Vector3> usedPowerPositions = SpawnObjects(highPowerZombiePrefab, highPowerCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedPowerPositions.Contains(pos));
        totalSpawned += usedPowerPositions.Count;

        Debug.Log($"[SpawnManager] 밤 스폰 완료 - 좀비 총 {totalSpawned}마리 스폰 (HP: {usedHpPositions.Count}, Speed: {usedSpeedPositions.Count}, Power: {usedPowerPositions.Count})");
    }

    // MARK: 모든 스폰 오브젝트 제거 (씬 전환 시 호출)
    public void ClearAll()
    {
        // 씬에서 실제로 스폰된 오브젝트만 파괴합니다. (Persistent Data는 유지)
        ClearItems();
        ClearZombies();
        ClearNPCs();
    }

    // MARK: 아이템만 제거
    private void ClearItems()
    {
        foreach (var item in spawnedItems) 
            if (item != null) Destroy(item);
        spawnedItems.Clear();
        itemManager?.ClearItems(); 
    }
    
    // 🌟 [추가] 외부에서 아이템이 주워졌을 때 영구 데이터에서 제거하는 함수
    /// <summary>
    /// 아이템이 주워졌을 때 영구적으로 제거합니다.
    /// </summary>
    /// <param name="itemObject">씬에서 파괴될 아이템 GameObject</param>
    public void RemovePersistentItem(GameObject itemObject)
    {
        // 씬에서 아이템이 파괴되기 전에, 해당 아이템의 위치를 기준으로 Persistent List에서 제거합니다.
        // float 비교는 오차를 감안하여 Vector3.Distance를 사용합니다.
        int index = persistentItems.FindIndex(data => Vector3.Distance(data.position, itemObject.transform.position) < 0.1f);

        if (index != -1)
        {
            persistentItems.RemoveAt(index);
            Debug.Log($"[SpawnManager] Persistent Item Data 제거 완료: {itemObject.name} at {itemObject.transform.position}");
        }
        else
        {
             Debug.LogWarning($"[SpawnManager] Persistent Item Data를 찾을 수 없습니다: {itemObject.name} at {itemObject.transform.position}");
        }
        
        // spawnedItems 리스트에서도 제거
        spawnedItems.Remove(itemObject);
        // 실제 GameObject 파괴는 이 함수를 호출한 쪽(예: Hero가 아이템을 주웠을 때)에서 수행해야 합니다.
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