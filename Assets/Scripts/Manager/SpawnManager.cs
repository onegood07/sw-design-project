// Assets/Scripts/Manager/SpawnManager.cs

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine.SceneManagement; 
using Random = UnityEngine.Random;

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

// 씬 전환 시 유지할 아이템 정보 구조체 (유지)
[System.Serializable]
public struct PersistentItemData
{
    public Vector3 position;
    public string prefabName; 
    public ItemType type;
}

// 🌟 씬 전환 시 유지할 NPC 정보 구조체 (⭐ 상태 필드 추가 및 삭제 플래그 추가)
[System.Serializable]
public struct PersistentNPCData
{
    public Vector3 position;
    public string prefabName; // 프리팹을 찾기 위한 이름 (복원 시 사용)
    
    // ⭐ [수정 반영]: 퀘스트 완료 상태
    public bool isQuestCompleted; 
    
    // ⭐ [수정 반영]: 퀘스트 완료 후 반복할 대화 노드의 인덱스
    public int repeatDialogueNodeIndex; 

    // ⭐ [추가]: 퀘스트 완료 후 다음 씬 로드 시 영구적으로 제거할지 여부
    public bool isMarkedForRemoval; 
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
    public GameObject highSpeedZombiePrefab; // HighSpeed 좀비 
    public GameObject highPowerZombiePrefab; // HighPower 좀비 
    
    [Header("Zombie Spawn Count")]
    public int defaultDayZombieCount = 60; // 복원 시 낮에 스폰할 기본 좀비 수

    [Header("Managers")]
    public ItemManager itemManager;   // 아이템 등록 및 관리 매니저

    [Header("스폰 개수 제한")]
    public int maxWeaponsOnField = 4;

    // 스폰 가능한 모든 위치
    private List<Vector3> allSpawnPositions = new List<Vector3>();

    // 🌟 씬 로드 시 파괴되지 않는 영구 데이터 리스트 (좀비는 제외)
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
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 🚨 ItemManager가 인스펙터에 할당되지 않았을 경우를 대비한 Null 체크
        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
            
            if (itemManager == null)
            {
                Debug.LogWarning("[SpawnManager] ItemManager 인스턴스를 씬에서 찾을 수 없습니다.");
            }
        }
    }

    // 🌟 씬이 로드될 때 호출되는 이벤트 등록
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (GameManager.Instance == null) 
        {
             Debug.LogError("[SpawnManager] GameManager 인스턴스를 찾을 수 없습니다. 씬 로드 로직 건너뜀.");
             return;
        }
        
        // 새 씬의 타일맵 정보를 다시 가져옵니다. (매 씬 로드 시 필수)
        GetSpawnPositions(); 
        
        // 🌟 쉘터 씬으로 이동 시, 현재 씬의 모든 스폰된 오브젝트를 클리어합니다.
        if (scene.name == GameManager.Instance.ShelterSceneName)
        {
            ClearAll(); 
            Debug.Log("[SpawnManager] 쉘터 진입: 씬 오브젝트 클리어 완료.");
        }
        
        // 🌟 메인 월드 씬으로 돌아왔을 때 복원 로직 실행
        else if (scene.name == GameManager.Instance.MainWorldSceneName) 
        {
            // 복원 전, 현재 씬에 남아있을 수 있는 오브젝트 클리어
            ClearAll();

            if (persistentItems.Count > 0 || persistentNPCs.Count > 0)
            {
                // 기존 데이터가 있다면 복원 (아이템/NPC 및 강제 좀비 스폰 포함)
                RestorePersistentObjects();
            }
            // 기존 데이터가 없다면 (첫 스폰이 필요하다면) GameManager에서 StartSpawnProcess를 호출해야 합니다.
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
        // 씬 로드시 타일맵 오브젝트를 다시 찾습니다.
        if (groundTilemap == null || collisionTilemap == null || !groundTilemap.gameObject.scene.isLoaded)
        {
            GameObject groundObj = GameObject.Find("GroundTilemap");
            GameObject collisionObj = GameObject.Find("CollisionTilemap");

            if (groundObj != null) groundTilemap = groundObj.GetComponent<Tilemap>();
            if (collisionObj != null) collisionTilemap = collisionObj.GetComponent<Tilemap>();

            if (groundTilemap == null || collisionTilemap == null)
            {
                 Debug.LogWarning("[SpawnManager] Tilemap이 할당되지 않았거나 씬에서 찾을 수 없습니다.");
                 return;
            }
        }

        BoundsInt bounds = groundTilemap.cellBounds; 
        TileBase[] allGroundTiles = groundTilemap.GetTilesBlock(bounds); 

        allSpawnPositions.Clear();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
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
         Debug.Log($"[SpawnManager] 스폰 가능한 위치 총 {allSpawnPositions.Count}개 계산 완료.");
    }

    // MARK: 오브젝트 스폰 공용 함수 (🌟 지속 데이터 기록 로직 포함)
    private List<Vector3> SpawnObjects(GameObject prefab, int count, List<Vector3> availablePositions, List<GameObject> outputList, ItemType? type = null)
    {
        List<Vector3> usedPositions = new List<Vector3>();
        List<Vector3> copy = new List<Vector3>(availablePositions);
        
        string prefabName = prefab.name.Replace("(Clone)", "").Trim(); 

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
            // NPC일 경우 Persistent NPC Data 기록 (type이 null이면서 spawnedNPCs에 추가된 경우)
            else if (type == null && outputList == spawnedNPCs)
            {
                // ⭐ [수정 반영]: 초기 NPC 생성 시 퀘스트 미완료 상태로 기록 및 제거 플래그 초기화
                persistentNPCs.Add(new PersistentNPCData
                {
                    position = spawnPos,
                    prefabName = prefabName,
                    isQuestCompleted = false,             // ⭐ 초기값
                    repeatDialogueNodeIndex = 0,          // ⭐ 초기값
                    isMarkedForRemoval = false            // ⭐ [추가] 초기값
                });
            }
            
            // 좀비는 영구 데이터를 기록하지 않습니다.

            usedPositions.Add(spawnPos);
            copy.RemoveAt(index); 
        }

        return usedPositions; 
    }
    
    // ⭐ [수정된 함수]: NPC가 퀘스트를 완료했을 때 Persistent Data를 업데이트하거나 제거하는 함수
    /// <summary>
    /// NPC의 퀘스트 완료 상태와 반복 대화 노드 인덱스를 영구 데이터에 업데이트합니다. 
    /// 퀘스트 완료 시 (isCompleted = true) 해당 NPC는 영구 데이터에 '제거 예정'으로 표시되며, 다음 씬 로드 시 영구적으로 제거됩니다.
    /// </summary>
    public void UpdatePersistentNPCData(GameObject npcObject, int repeatNodeIndex, bool isCompleted)
    {
        Vector3 npcPos = npcObject.transform.position;
        
        // 위치를 기반으로 영구 데이터 리스트에서 해당 NPC를 찾습니다.
        int index = persistentNPCs.FindIndex(data => Vector3.Distance(data.position, npcPos) < 0.1f);

        if (index != -1)
        {
            PersistentNPCData updatedData = persistentNPCs[index];
            
            updatedData.isQuestCompleted = isCompleted;
            updatedData.repeatDialogueNodeIndex = repeatNodeIndex;

            if (isCompleted) 
            {
                // ⭐ [수정] 즉시 제거하지 않고, 다음 씬 로드 시 제거되도록 플래그만 설정
                updatedData.isMarkedForRemoval = true;
                Debug.Log($"[SpawnManager] 퀘스트 완료 NPC '{npcObject.name}' 영구 데이터에 '제거 예정' 플래그 설정 완료.");
            }
            
            // 수정된 데이터를 다시 리스트에 저장
            persistentNPCs[index] = updatedData;

            Debug.Log($"[SpawnManager] Persistent NPC Data 업데이트 완료: {npcObject.name} (Quest Completed: {isCompleted}, Repeat Node: {repeatNodeIndex})");
        }
        else
        {
             // 퀘스트 완료 NPC가 영구 데이터에 없을 경우 (두 번 이상 완료 보고 시도 등)
             Debug.LogWarning($"[SpawnManager] Persistent NPC Data를 찾을 수 없어 업데이트/제거 실패: {npcObject.name} at {npcPos}");
        }
    }


    // MARK: - 🌟 지속 오브젝트 복원 함수 (⭐ 삭제 예정 NPC 필터링 로직 추가)
    public void RestorePersistentObjects()
    {
        Debug.Log("[SpawnManager] RestorePersistentObjects 시작.");
        
        // 씬에서 이미 스폰된 오브젝트 목록 초기화 (안정성을 위해 재호출)
        ClearItems();
        ClearNPCs();
        ClearZombies(); 
        
        // ⭐ [추가] 영구 제거될 NPC 목록을 따로 저장
        List<PersistentNPCData> npcsToKeep = new List<PersistentNPCData>();
        
        List<Vector3> occupiedPositions = new List<Vector3>();

        // 1. 아이템 복원
        foreach (var itemData in persistentItems)
        {
            GameObject prefabToSpawn = GetItemPrefabByName(itemData.prefabName);
            
            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, itemData.position, Quaternion.identity);
                spawnedItems.Add(obj);
                itemManager?.RegisterSpawnedItem(obj, itemData.type);
                occupiedPositions.Add(itemData.position);
            }
        }
        
        // 2. NPC 복원 (⭐ 삭제 예정 NPC는 복원하지 않고, 리스트에서 영구 제거)
        int restoredNpcCount = 0;
        foreach (var npcData in persistentNPCs)
        {
            if (npcData.isMarkedForRemoval)
            {
                // 영구 제거 대상. 복원하지 않고 리스트에 추가하지 않음.
                Debug.Log($"[SpawnManager] '제거 예정'으로 표시된 NPC '{npcData.prefabName}'를 영구 데이터에서 제거하고 복원하지 않습니다.");
                continue; 
            }

            // 복원 대상
            GameObject prefabToSpawn = GetNpcPrefabByName(npcData.prefabName);

            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, npcData.position, Quaternion.identity);
                spawnedNPCs.Add(obj);
                occupiedPositions.Add(npcData.position);
                restoredNpcCount++;

                // 복원된 NPC에게 완료 상태 및 반복 노드 설정 전달
                DialogueNPC npcComponent = obj.GetComponent<DialogueNPC>();
                if (npcComponent != null)
                {
                    npcComponent.RestoreState(npcData.isQuestCompleted, npcData.repeatDialogueNodeIndex); 
                }
                
                // 복원된 NPC는 유지할 목록에 추가
                npcsToKeep.Add(npcData);
            }
        }
        
        // 영구 데이터 리스트를 유지할 NPC 목록으로 덮어씁니다 (실제 영구 제거)
        persistentNPCs = npcsToKeep;
        
        // 3. 좀비 강제 스폰 (아이템/NPC 복원 직후 낮 좀비 스폰 보장)
        Debug.Log($"[SpawnManager] 복원 후 낮 좀비 스폰 강제 실행: {defaultDayZombieCount}마리.");
        SpawnZombiesDuringRestore(defaultDayZombieCount, occupiedPositions);
        
        Debug.Log($"[SpawnManager] 오브젝트 복원 및 좀비 스폰 완료: 아이템 {spawnedItems.Count}개, NPC {restoredNpcCount}명, 좀비 {spawnedZombies.Count}마리");
    }

    // 좀비 스폰 로직 (쉘터 복원 시 사용)
    private void SpawnZombiesDuringRestore(int zombieCount, List<Vector3> occupiedPositions)
    {
        if (allSpawnPositions.Count == 0)
        {
             GetSpawnPositions();
            if (allSpawnPositions.Count == 0)
            {
                 Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없어 좀비 스폰을 건너뜁니다.");
                 return;
            }
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 아이템/NPC가 복원된 위치 제외
        remainingPositions.RemoveAll(pos => occupiedPositions.Contains(pos));

        // 현재 날짜에 따라 특수좀비 스폰 여부 결정 (2일차 이상부터 특수좀비 스폰)
        bool shouldSpawnSpecialZombies = GameManager.Instance != null && GameManager.Instance.CurrentDay >= GameDays.SecondDay;
        
        if (shouldSpawnSpecialZombies)
        {
            // 2일차 이상: Normal, HighSpeed, HighPower 세 종류에 균등하게 분배
            int countPerType = zombieCount / 3;
            int normalCount = countPerType;
            int highSpeedCount = countPerType;
            int highPowerCount = zombieCount - normalCount - highSpeedCount;

            // Normal 좀비 스폰
            List<Vector3> usedNormalPositions = SpawnObjects(normalZombiePrefab, normalCount, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedNormalPositions.Contains(pos));

            // HighSpeed 좀비 스폰
            List<Vector3> usedSpeedPositions = SpawnObjects(highSpeedZombiePrefab, highSpeedCount, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedSpeedPositions.Contains(pos));

            // HighPower 좀비 스폰
            List<Vector3> usedPowerPositions = SpawnObjects(highPowerZombiePrefab, highPowerCount, remainingPositions, spawnedZombies);
            
            Debug.Log($"[SpawnManager] 복원 시 좀비 스폰 완료 (2일차 이상): Normal {usedNormalPositions.Count}마리, HighSpeed {usedSpeedPositions.Count}마리, HighPower {usedPowerPositions.Count}마리");
        }
        else
        {
            // 1일차: Normal 좀비만 스폰
            List<Vector3> usedZombiePositions = SpawnObjects(normalZombiePrefab, zombieCount, remainingPositions, spawnedZombies);
            Debug.Log($"[SpawnManager] 복원 시 좀비 스폰 완료 (1일차): Normal {usedZombiePositions.Count}마리");
            
            if (usedZombiePositions.Count < zombieCount)
            {
                Debug.LogWarning($"[SpawnManager] 요청된 좀비 수({zombieCount})보다 적은 수({usedZombiePositions.Count})의 좀비만 스폰되었습니다. (위치 부족)");
            }
        }
    }


    // 🌟 프리팹 이름으로 아이템 프리팹을 찾는 헬퍼 함수
    private GameObject GetItemPrefabByName(string name)
    {
        string cleanName = name.Replace("(Clone)", "").Trim(); 

        foreach (var info in itemInfos)
        {
            if (info.prefab != null && info.prefab.name.Replace("(Clone)", "").Trim() == cleanName)
            {
                return info.prefab;
            }
        }
        return null;
    }

    // 🌟 프리팹 이름으로 NPC 프리팹을 찾는 헬퍼 함수
    private GameObject GetNpcPrefabByName(string name)
    {
        string cleanName = name.Replace("(Clone)", "").Trim(); 

        foreach (var prefab in npcPrefabs)
        {
            if (prefab != null && prefab.name.Replace("(Clone)", "").Trim() == cleanName)
            {
                return prefab;
            }
        }
        return null;
    }


   // MARK: 아이템, NPC, 좀비 스폰 함수 (🌟 영구 데이터가 없을 때만 실행)
    public void StartSpawnProcess(int totalItemCount, int npcCount, int zombieCount)
    {
        // 🚨 수정: 아이템/NPC 영구 데이터가 있다면 초기 스폰 전체를 건너뜁니다.
        if (persistentItems.Count > 0 || persistentNPCs.Count > 0)
        {
            Debug.LogWarning("[SpawnManager] 영구 데이터가 존재하여 StartSpawnProcess를 건너뛰었습니다. 복원 로직이 우선 적용됩니다.");
            return;
        }
        
        // 씬에 오브젝트가 남아있다면 ClearAll() 호출 (첫 게임 시작 시)
        if (spawnedItems.Count > 0 || spawnedNPCs.Count > 0 || spawnedZombies.Count > 0)
        {
            ClearAll();
        }

        if (allSpawnPositions.Count == 0)
        {
             GetSpawnPositions();
            if (allSpawnPositions.Count == 0)
            {
                 Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다.");
                 return;
            }
        }

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);
        int spawnedItemCount = 0; // 실제로 스폰된 아이템 수 카운트

        // ⭐⭐⭐ 0단계: 총기류(Weapon) 4개 고정 스폰 ⭐⭐⭐
        
        // Weapon 타입 아이템 정보 중 첫 번째 프리팹을 찾습니다.
        ItemSpawnInfo? weaponInfo = itemInfos.FirstOrDefault(info => info.type == ItemType.Weapon && info.prefab != null);
        
        // 총기류 고정 스폰 개수 설정 (요청하신 대로 4개)
        const int fixedWeaponSpawnCount = 4;
        
        if (weaponInfo.HasValue)
        {
            GameObject weaponPrefab = weaponInfo.Value.prefab;
            string prefabName = weaponPrefab.name.Replace("(Clone)", "").Trim();

            int actualSpawnedWeapons = 0;

            for (int i = 0; i < fixedWeaponSpawnCount; i++)
            {
                if (remainingPositions.Count == 0 || spawnedItemCount >= totalItemCount) break;

                int posIndex = Random.Range(0, remainingPositions.Count);
                Vector3 spawnPos = remainingPositions[posIndex];
                remainingPositions.RemoveAt(posIndex);
                
                // 오브젝트 스폰 및 영구 데이터 기록
                GameObject obj = Instantiate(weaponPrefab, spawnPos, Quaternion.identity);
                spawnedItems.Add(obj);

                if (itemManager != null)
                    itemManager.RegisterSpawnedItem(obj, ItemType.Weapon);
                        
                // Persistent Item Data 기록
                persistentItems.Add(new PersistentItemData
                {
                    position = spawnPos,
                    prefabName = prefabName,
                    type = ItemType.Weapon
                });

                actualSpawnedWeapons++;
                spawnedItemCount++;
            }
            
            Debug.Log($"[SpawnManager] 0단계: 총기류 {actualSpawnedWeapons}개 고정 스폰 완료.");
        } 
        else
        {
            Debug.LogWarning("[SpawnManager] 총기류(ItemType.Weapon) 프리팹을 itemInfos에서 찾을 수 없어 고정 스폰을 건너뜁니다.");
        }


        // ---------------------------------------------------------------------------------
        // 1단계: GameManager의 납입 요구 목록에 있는 아이템을 필드에 모두 소환
        // ---------------------------------------------------------------------------------
        if (GameManager.Instance != null && GameManager.Instance.CurrentRequiredItemsData.Count > 0)
        {
            int requiredItemSpawnedCount = 0;
            
            // 납입품 리스트를 순회하며 요구 수량만큼 스폰
            foreach (var requiredItem in GameManager.Instance.CurrentRequiredItemsData)
            {
                // **⭐⭐⭐ Item 클래스 정의가 있다고 가정합니다. ⭐⭐⭐**
                Item requiredItemData = requiredItem.Key as Item; 
                int requiredQuantity = requiredItem.Value;
                
                if (requiredItemData == null) continue;

                string targetName = requiredItemData.itemName.Replace("(Clone)", "").Trim(); 
                
                // -------------------------------------------------------------------------
                
                ItemSpawnInfo? targetInfo = null;
                foreach (var info in itemInfos)
                {
                    if (info.prefab != null && info.prefab.name.Replace("(Clone)", "").Trim() == targetName)
                    {
                        targetInfo = info;
                        break;
                    }
                }

                if (targetInfo.HasValue && targetInfo.Value.prefab != null)
                {
                    GameObject prefab = targetInfo.Value.prefab;
                    ItemType itemType = targetInfo.Value.type;

                    for (int i = 0; i < requiredQuantity; i++)
                    {
                        if (remainingPositions.Count == 0 || spawnedItemCount >= totalItemCount) break;

                        int posIndex = Random.Range(0, remainingPositions.Count);
                        Vector3 spawnPos = remainingPositions[posIndex];
                        remainingPositions.RemoveAt(posIndex);
                        
                        // 🚨 오브젝트 스폰 및 영구 데이터 기록
                        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                        spawnedItems.Add(obj);

                        if (itemManager != null)
                            itemManager.RegisterSpawnedItem(obj, itemType);
                                
                        // 🌟 Persistent Item Data 기록
                        persistentItems.Add(new PersistentItemData
                        {
                            position = spawnPos,
                            prefabName = prefab.name.Replace("(Clone)", "").Trim(),
                            type = itemType
                        });

                        requiredItemSpawnedCount++;
                        spawnedItemCount++;
                    }
                } else {
                     Debug.LogWarning($"[SpawnManager] 요구 아이템 '{targetName}'에 해당하는 프리팹을 itemInfos에서 찾을 수 없습니다. 스폰 생략.");
                }
                if (remainingPositions.Count == 0 || spawnedItemCount >= totalItemCount) break;
            }

            Debug.Log($"[SpawnManager] 1단계: 납입 요구 아이템 {requiredItemSpawnedCount}개 우선 스폰 완료.");
        }
        else
        {
            Debug.Log("[SpawnManager] GameManager 인스턴스를 찾을 수 없거나 요구 납입품이 없습니다.");
        }


        // ---------------------------------------------------------------------------------
        // 2단계: 남은 수량(totalItemCount - spawnedItemCount)만큼 나머지 아이템을 확률적으로 스폰
        // ---------------------------------------------------------------------------------
        
        int remainingItemsToSpawn = totalItemCount - spawnedItemCount;
        float totalWeight = itemInfos.Sum(info => info.ratio);

        // ⭐ [핵심]: 0단계와 1단계를 거치면서 필드에 스폰된 총기류 개수를 정확히 계산합니다.
        int currentWeaponCount = persistentItems.Count(data => data.type == ItemType.Weapon);
        
        for (int i = 0; i < remainingItemsToSpawn; i++)
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
                
                // ⭐ [핵심 수정]: 총기류(Weapon) 스폰 개수 제한 확인 및 건너뛰기
                if (selectedInfo.type == ItemType.Weapon)
                {
                    if (currentWeaponCount >= fixedWeaponSpawnCount)
                    {
                        Debug.Log($"[SpawnManager] 총기류({selectedInfo.prefab.name}) 최대 스폰 개수({fixedWeaponSpawnCount})에 도달했습니다. 2단계 확률 스폰을 건너뜁니다.");
                        continue; 
                    }
                    // 스폰이 허용되면 카운터를 증가시킵니다.
                    currentWeaponCount++; 
                }
                
                if (selectedInfo.prefab == null) continue;

                int posIndex = Random.Range(0, remainingPositions.Count);
                Vector3 spawnPos = remainingPositions[posIndex];
                remainingPositions.RemoveAt(posIndex);
                
                string prefabName = selectedInfo.prefab.name.Replace("(Clone)", "").Trim();


                GameObject obj = Instantiate(selectedInfo.prefab, spawnPos, Quaternion.identity);
                spawnedItems.Add(obj);

                if (itemManager != null)
                    itemManager.RegisterSpawnedItem(obj, selectedInfo.type);
                        
                // 🌟 Persistent Item Data 기록
                persistentItems.Add(new PersistentItemData
                {
                    position = spawnPos,
                    prefabName = prefabName,
                    type = selectedInfo.type
                });
                spawnedItemCount++;
            }
        }
        Debug.Log($"[SpawnManager] 2단계: 확률적 아이템 {remainingItemsToSpawn}개 스폰 시도. 실제 스폰된 아이템 총 {spawnedItemCount}개.");


        // ---------------------------------------------------------------------------------
        // ⭐ [수정된 3단계]: NPC 스폰 (고정 2명 + 일반 최대 5명)
        // ---------------------------------------------------------------------------------
        
        // 고정 소환할 NPC 목록 (프리팹 이름으로 식별)
        string[] requiredNpcNames = { "ExchangeNPC", "RecipeNPC" }; 
        List<GameObject> availableNpcs = new List<GameObject>(npcPrefabs);
        List<GameObject> npcsToSpawn = new List<GameObject>();
        int totalNpcSpawned = 0;

        // 3-1. 고정 NPC 무조건 스폰 및 영구 데이터 기록 (2명)
        foreach (string requiredName in requiredNpcNames)
        {
            GameObject requiredPrefab = GetNpcPrefabByName(requiredName); 

            if (requiredPrefab != null)
            {
                if (remainingPositions.Count == 0)
                {
                    Debug.LogWarning($"[SpawnManager] 고정 NPC '{requiredName}' 스폰 시도 실패: 스폰 가능한 위치가 없습니다.");
                    break; 
                }

                // SpawnObjects를 사용하여 생성 및 persistentNPCs에 기록
                List<Vector3> usedPos = SpawnObjects(requiredPrefab, 1, remainingPositions, spawnedNPCs); 
                remainingPositions.RemoveAll(pos => usedPos.Contains(pos));
                
                // availableNpcs 리스트에서 해당 프리팹을 제거하여 3-2단계에서 중복 스폰 방지
                availableNpcs.RemoveAll(p => p != null && p.name.Replace("(Clone)", "").Trim() == requiredName);
                totalNpcSpawned++;

                Debug.Log($"[SpawnManager] 고정 NPC '{requiredName}' 스폰 완료.");
            }
            else
            {
                Debug.LogWarning($"[SpawnManager] 고정 NPC '{requiredName}' 프리팹을 npcPrefabs에서 찾을 수 없습니다.");
            }
        }

        // 3-2. 일반 NPC 추가 스폰 (나머지 NPC 중 최대 5명)
        const int additionalNpcCount = 5;
        int actualAdditionalCount = 0; 
        int currentAvailableNpcCount = availableNpcs.Count;

        // 스폰할 수 있는 최대 인원은 '남은 NPC 수' 또는 '요청된 추가 수(5명)' 중 작은 값
        int countToSelect = Mathf.Min(additionalNpcCount, currentAvailableNpcCount); 

        for (int i = 0; i < countToSelect; i++)
        {
            if (availableNpcs.Count == 0) break;

            int randomIndex = Random.Range(0, availableNpcs.Count);
            GameObject selectedNpc = availableNpcs[randomIndex];

            npcsToSpawn.Add(selectedNpc);
            availableNpcs.RemoveAt(randomIndex); // 중복 방지를 위해 제거
        }

        // 선택된 일반 NPC들을 스폰합니다.
        foreach (GameObject npcPrefab in npcsToSpawn)
        {
            if (remainingPositions.Count == 0)
            {
                Debug.LogWarning($"[SpawnManager] 일반 NPC '{npcPrefab.name}' 스폰 시도 실패: 스폰 가능한 위치가 없습니다.");
                break;
            }
            
            // SpawnObjects를 사용하여 생성 및 persistentNPCs에 (미완료 상태로) 기록
            List<Vector3> usedNpcPositions = SpawnObjects(npcPrefab, 1, remainingPositions, spawnedNPCs); 
            remainingPositions.RemoveAll(pos => usedNpcPositions.Contains(pos));
            actualAdditionalCount++;
            totalNpcSpawned++;
        }

        Debug.Log($"[SpawnManager] 3단계: NPC 스폰 완료. 총 {totalNpcSpawned}명 (고정 2명 + 일반 {actualAdditionalCount}명).");


        // 4. 좀비 스폰 (기존 로직 유지)
        Debug.Log($"[SpawnManager] 4단계: 초기 좀비 스폰 시작: {zombieCount}마리 (Normal).");
        List<Vector3> usedZombiePositions = SpawnObjects(normalZombiePrefab, zombieCount, remainingPositions, spawnedZombies);
        remainingPositions.RemoveAll(pos => usedZombiePositions.Contains(pos));
        Debug.Log($"[SpawnManager] 초기 좀비 스폰 완료: {usedZombiePositions.Count}마리.");
    }

    // MARK: 좀비만 스폰 (밤 페이즈용 - 기존 좀비를 유지하고 추가 스폰)
    public void SpawnZombiesOnly(int totalTargetZombieCount)
    {
        if (allSpawnPositions.Count == 0)
        {
            GetSpawnPositions(); 
            if (allSpawnPositions.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] 스폰 가능한 위치가 없습니다. 밤 좀비 스폰 생략.");
                return;
            }
        }
        
        // 1. 이미 존재하는 좀비 수를 확인하고, 새로 스폰할 좀비 수를 계산합니다.
        int existingZombieCount = spawnedZombies.Count;
        int newZombiesToSpawn = totalTargetZombieCount - existingZombieCount;
        
        if (newZombiesToSpawn <= 0)
        {
            Debug.Log($"[SpawnManager] 목표 좀비 수({totalTargetZombieCount})가 이미 존재하는 좀비 수({existingZombieCount})보다 적거나 같아서 새로운 좀비를 스폰하지 않습니다.");
            return;
        }
        
        Debug.Log($"[SpawnManager] 밤 추가 스폰 시작: {newZombiesToSpawn}마리.");

        List<Vector3> remainingPositions = new List<Vector3>(allSpawnPositions);

        // 2. 아이템/NPC/기존 좀비 위치를 피하기 위해 남은 위치를 계산
        remainingPositions.RemoveAll(pos =>
            persistentItems.Exists(item => Vector3.Distance(item.position, pos) < 0.1f) ||
            persistentNPCs.Exists(npc => Vector3.Distance(npc.position, pos) < 0.1f)
        );

        // 기존 좀비의 위치도 제외합니다.
        foreach(var zombie in spawnedZombies)
        {
            if (zombie != null)
            {
                remainingPositions.RemoveAll(pos => Vector3.Distance(zombie.transform.position, pos) < 0.1f);
            }
        }
        
        if (remainingPositions.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 기존 오브젝트/좀비로 인해 새로운 좀비를 스폰할 위치가 없습니다.");
            return;
        }

        // 3. 새로운 좀비 스폰 - 2일차부터는 특수좀비(HighSpeed, HighPower) 포함
        int totalSpawned = 0;
        bool shouldSpawnSpecialZombies = GameManager.Instance != null && GameManager.Instance.CurrentDay >= GameDays.SecondDay;

        if (shouldSpawnSpecialZombies)
        {
            // 2일차 이상: Normal, HighSpeed, HighPower 세 종류에 균등하게 분배
            int countPerType = newZombiesToSpawn / 3;
            int normalCount = countPerType;
            int highSpeedCount = countPerType;
            int highPowerCount = newZombiesToSpawn - normalCount - highSpeedCount;

            // Normal Zombie 추가 스폰
            List<Vector3> usedNormalPositions = SpawnObjects(normalZombiePrefab, normalCount, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedNormalPositions.Contains(pos));
            totalSpawned += usedNormalPositions.Count;

            // High Speed Zombie 추가 스폰
            List<Vector3> usedSpeedPositions = SpawnObjects(highSpeedZombiePrefab, highSpeedCount, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedSpeedPositions.Contains(pos));
            totalSpawned += usedSpeedPositions.Count;

            // High Power Zombie 추가 스폰
            List<Vector3> usedPowerPositions = SpawnObjects(highPowerZombiePrefab, highPowerCount, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedPowerPositions.Contains(pos));
            totalSpawned += usedPowerPositions.Count;
        }
        else
        {
            // 1일차: Normal 좀비만 스폰
            List<Vector3> usedNormalPositions = SpawnObjects(normalZombiePrefab, newZombiesToSpawn, remainingPositions, spawnedZombies);
            remainingPositions.RemoveAll(pos => usedNormalPositions.Contains(pos));
            totalSpawned += usedNormalPositions.Count;
        }

        Debug.Log($"[SpawnManager] 밤 스폰 완료 - 새로운 좀비 총 {totalSpawned}마리 추가 스폰. 현재 씬 총 좀비 수: {spawnedZombies.Count}");
        if (shouldSpawnSpecialZombies)
        {
            Debug.Log($"  - (2일차 이상) 특수좀비 포함 스폰");
        }
        else
        {
            Debug.Log($"  - (1일차) Normal 좀비만 스폰");
        }
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
    
    // 🌟 외부에서 아이템이 주워졌을 때 영구 데이터에서 제거하는 함수
    public void RemovePersistentItem(GameObject itemObject)
    {
        // 씬에서 아이템이 파괴되기 전에, 해당 아이템의 위치를 기준으로 Persistent List에서 제거합니다.
        Vector3 itemPos = itemObject.transform.position;
        int index = persistentItems.FindIndex(data => Vector3.Distance(data.position, itemPos) < 0.1f);

        if (index != -1)
        {
            persistentItems.RemoveAt(index);
            Debug.Log($"[SpawnManager] Persistent Item Data 제거 완료: {itemObject.name} at {itemPos}");
        }
        else
        {
             Debug.LogWarning($"[SpawnManager] Persistent Item Data를 찾을 수 없습니다: {itemObject.name} at {itemPos}");
        }
        
        // spawnedItems 리스트에서도 제거
        spawnedItems.Remove(itemObject);
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

    // MARK: - 영구 데이터 초기화 함수
    public void ResetPersistentData()
    {
        // 영구 데이터 리스트를 비웁니다.
        persistentItems.Clear();
        persistentNPCs.Clear();
        
        // 현재 씬에 스폰된 모든 오브젝트를 클리어합니다.
        ClearAll(); 
        
        Debug.Log("[SpawnManager] 모든 영구 스폰 데이터(아이템, NPC)가 초기화되었습니다.");
    }
}