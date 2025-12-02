using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using Random = UnityEngine.Random;
using Unity.Android.Gradle;

// 게임 상태 관련 Enum 정의
public enum GameEnding { None, Happy, GameOver, Bad } // 엔딩 종류
public enum GameDays { FirstDay, SecondDay, ThirdDay, FourthDay } // 게임 일차
public enum Phase { Day, Night } // 낮/밤 페이즈

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance;

    // 게임 상태 변수
    public GameEnding CurrentEnding { get; private set; } // 현재 엔딩 상태
    public GameDays CurrentDay { get; private set; } // 현재 일차
    public Phase CurrentPhase { get; private set; } // 현재 페이즈

    public bool IsInShelter { get; set; } = false; // 현재 씬이 쉘터 내부인지 확인

    // MARK: 납입품 관련 설정
    [Header("Item Submission Settings")]
    // 일차별로 생성된 납입 요구 아이템 목록 (아이템 타입, 요구 수량)
    public Dictionary<ItemType, int> CurrentRequiredItems { get; private set; } = new Dictionary<ItemType, int>();
    public int BaseRequiredAmount = 5; // 기본 요구 수량
    public int MaxRequiredIncrease = 3; // 일차별 최대 증가 수량

    // MARK: 좀비 능력치 배율 설정 (밤페이즈 특수 좀비용도)
    [Header("Zombie Multipliers")]
    // 밤에 적용할 이동 속도 배율 (ex. 1.5면 50% 증가)
    public float NightSpeedMultiplier = 1.3f; 
    // 밤에 적용할 체력 배율 
    public float NightHpMultiplier = 1.2f; 
    // 밤에 적용할 공격력 배율 
    public float NightPowerMultiplier = 1.5f; 

    // 점수 관련
    [Header("Scores")]
    public int ShelterItemScore = 0; // 납입품 점수
    public int SurvivorScore = 10; // 생존자 수 점수

    // 스폰 관련 설정
    [Header("Spawn Settings")]
    public SpawnManager spawnManager; // 스폰 관리 매니저
    public int ItemSpawnCount = 5;    // 낮/밤 아이템 스폰 수
    public int NPCSpawnCount = 3;     // NPC 스폰 수
    public int BaseZombieSpawnCount = 10; // 초기 좀비 스폰 수
    private int CurrentZombieSpawnCount;  // 현재 좀비 스폰 수

    // 페이즈 지속 시간
    [Header("Phase Duration")]
    public float dayDuration = 1.0f;
    public float nightDuration = 20.0f;
    
    // 씬 이름 설정
    [Header("Scene Settings")]
    public string MainWorldSceneName = "Main";
    public string ShelterSceneName = "InsideShelter"; 

    // 게임엔딩 UI
    [Header("GameEnding Settings")]
    public GameObject gameOverPanel; // 게임오버 UI
    public GameObject uiRoot;        // 기본 UI 루트

    private Coroutine gameLoopCoroutine; // 전체 게임 루프 코루틴

    void Awake()
    {
        // 게임 시작 시 엔딩 초기화
        CurrentEnding = GameEnding.None;

        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않음
        }
        else 
        {
            Destroy(gameObject); // 중복 제거
        }
    }

    void Start()
    {
        // 초기 게임 상태 설정 -> 1일차 낮
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        CurrentZombieSpawnCount = BaseZombieSpawnCount; // 초기 좀비 수

        ApplyGlobalLight(); // 조명 반영
        GenerateRequiredItems();  // 아이템 납입 리스트 생성

        // 게임 전체 루프 시작
        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
    }

    // MARK: 플레이어 사망 처리
    public void PlayerDied()
    {
        CurrentEnding = GameEnding.GameOver;
        Debug.Log("[GameManager] 게임 오버 엔딩");

        // 기존 UI 비활성화
        if (uiRoot != null)
            uiRoot.SetActive(false);

        // 게임 오버 UI 표시
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 게임 루프 중단
        if (gameLoopCoroutine != null)
            StopCoroutine(gameLoopCoroutine);
    }

    // MARK: 글로벌 라이트 적용
    public void ApplyGlobalLight()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == ShelterSceneName)
        {
            Debug.Log($"[GameManager] {currentScene} 씬은 쉘터이므로 조명을 변경하지 않습니다.");
            return; // 쉘터에서는 조명 변경하지 않음
        }

        if (currentScene == MainWorldSceneName)
        {
            // 낮/밤 페이즈에 따라 조명 업데이트
            LightController.Instance?.UpdateGlobalLight(CurrentPhase); 
        }
        else
        {
            Debug.Log($"[GameManager] {currentScene} 씬은 월드가 아니므로 조명을 변경하지 않습니다.");
        }
    }

    // MARK: 전체 게임 루프 코루틴
    IEnumerator GameLoopCoroutine()
    {
        // 마지막 날(4일차)까지 반복
        while (CurrentDay <= GameDays.FourthDay)
        {
            // 낮 페이즈 시작
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");
            ApplyGlobalLight(); // 조명 반영
            StartDayPhase();    // 낮 페이즈 로직 시작
            yield return new WaitForSeconds(dayDuration); // 낮 지속

            // 밤 페이즈 시작
            CurrentPhase = Phase.Night;
            CurrentZombieSpawnCount += 20; // 밤마다 좀비 증가
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");
            ApplyGlobalLight();
            StartNightPhase();
            yield return new WaitForSeconds(nightDuration); // 밤 지속

            // 다음 날로 전환
            NextDay();
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }

    // MARK: 낮 페이즈 로직
    void StartDayPhase()
    {
        if (!IsInShelter)
        {
            spawnManager.ClearAll(); // 기존 스폰 초기화

            // 일차별 낮 아이템 비율 설정
            switch (CurrentDay)
            {
                case GameDays.FirstDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.SecondDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.ThirdDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.FourthDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
            }

            // 아이템/NPC/좀비 스폰
            spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
        } 
        else
        {
            Debug.Log("[GameManager] 쉘터에서는 낮 스폰 생략");
        }
    }

    // MARK: 밤 페이즈 로직
    void StartNightPhase()
    {
        if (!IsInShelter)
        {
            // 일차별 밤 아이템 비율 설정
            switch (CurrentDay)
            {
                case GameDays.FirstDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.SecondDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.ThirdDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
                case GameDays.FourthDay: 
                    SetItemSectorRatios(0.3f, 0.1f, 0.1f,0.1f,0.2f,0.2f);
                    break;
            }

            spawnManager.SpawnZombiesOnly(CurrentZombieSpawnCount); // 좀비만 스폰
        } 
        else
        {
            Debug.Log("[GameManager] 쉘터에서는 밤 스폰 생략");
        }
    }
    
    // MARK: 아이템별 스폰 확률 설정
    void SetItemSectorRatios(float heal, float weapon, float lantern,float quest,float material,float submit)
    {
        if (spawnManager != null && spawnManager.ItemSectorInfos != null && spawnManager.itemInfos != null &&spawnManager.itemInfos.Length >= 3)
        {
            spawnManager.ItemSectorInfos[0].ratio = heal;
            spawnManager.ItemSectorInfos[1].ratio = weapon;
            spawnManager.ItemSectorInfos[2].ratio = lantern;
            spawnManager.ItemSectorInfos[3].ratio = quest;
            spawnManager.ItemSectorInfos[4].ratio = material;
            spawnManager.ItemSectorInfos[5].ratio = submit;
            SetItemRatios();
        }
    }
    void SetItemRatios()
    {
        for(int i = 0; i < spawnManager.itemInfos.Length; i++)
        {
            switch (spawnManager.itemInfos[i].type)
            {
                case ItemType.Heal:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[0].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
                case ItemType.Weapon:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[1].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
                case ItemType.Lantern:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[2].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
                case ItemType.Quest:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[3].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
                case ItemType.Material:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[4].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
                case ItemType.Submit:
                    spawnManager.itemInfos[i].ratio = spawnManager.ItemSectorInfos[5].ratio * spawnManager.itemInfos[i].baseRatio;
                    break;
            }
        }
    }

    // MARK: 일차별 랜덤 납입품 목록 생성 로직
    void GenerateRequiredItems()
    {
        CurrentRequiredItems.Clear(); // 이전 날의 목록 초기화

        // 현재 일차에 따른 요구 수량 증가 폭 계산
        int dayIndex = (int)CurrentDay; // 0, 1, 2, 3
        
        // 요구 수량 - 기본 수량 + 일차에 비례한 랜덤 증가량
        int baseAmount = BaseRequiredAmount + Random.Range(0, dayIndex * MaxRequiredIncrease);

        // 납입 요구 아이템 타입 목록 (현재는 모든 타입)
        ItemType[] allTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));
        
        // 요구할 아이템 개수를 랜덤하게 결정
        int requiredItemCount = Random.Range(1, allTypes.Length); 
        
        // 아이템 타입 리스트를 섞기 (랜덤하게 선택하기 위해)
        List<ItemType> shuffledTypes = new List<ItemType>(allTypes);
        // 셔플 알고리즘 간소화
        for (int i = 0; i < shuffledTypes.Count; i++)
        {
            ItemType temp = shuffledTypes[i];
            int randomIndex = Random.Range(i, shuffledTypes.Count);
            shuffledTypes[i] = shuffledTypes[randomIndex];
            shuffledTypes[randomIndex] = temp;
        }

        // 섞인 목록에서 필요한 개수만큼 선택
        for (int i = 0; i < requiredItemCount; i++)
        {
            ItemType type = shuffledTypes[i];
            
            // 요구 수량에 약간의 랜덤 변화 주기
            int finalAmount = baseAmount + Random.Range(-1, 2); // +-1 정도의 변화
            if (finalAmount < 1) finalAmount = 1; // 최소 1개 이상 요구
            
            CurrentRequiredItems.Add(type, finalAmount);
        }
        
        // 디버그 출력
        Debug.Log($"[GameManager] {CurrentDay} 납입 요구 목록 생성:");
        foreach (var item in CurrentRequiredItems)
        {
            Debug.Log($" - {item.Key} : {item.Value}개");
        }
    }

    // MARK: 다음 날로 전환
    void NextDay()
    {
        switch (CurrentDay)
        {
            case GameDays.FirstDay: 
                CurrentDay = GameDays.SecondDay; 
                CurrentZombieSpawnCount += 10; 
                break;
            case GameDays.SecondDay: 
                CurrentDay = GameDays.ThirdDay; 
                CurrentZombieSpawnCount += 10; 
                break;
            case GameDays.ThirdDay: 
                CurrentDay = GameDays.FourthDay; 
                break;
            case GameDays.FourthDay: 
                CurrentDay++;
                break;
        }

        Debug.Log($"다음 날: {CurrentDay}, 좀비 수: {CurrentZombieSpawnCount}");

        // 다음 날 낮페이즈가 시작되기 전 목록 준비
        if (CurrentDay <= GameDays.FourthDay)
        {
            GenerateRequiredItems();
        }
    }

    // MARK: 씬 로드 시 실행
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 쉘터 내부 여부 확인
        IsInShelter = (scene.name == ShelterSceneName);

        // 월드 씬이면 스폰 매니저 연결
        if (scene.name == MainWorldSceneName)
            spawnManager = Object.FindFirstObjectByType<SpawnManager>();

        ApplyGlobalLight(); // 씬 로드 시 조명 반영
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 이벤트 구독
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // 이벤트 구독 해제
    }

}
