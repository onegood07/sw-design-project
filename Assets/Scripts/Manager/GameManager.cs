using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 

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

    // 점수 관련
    [Header("Scores")]
    public int ShelterItemScore { get; private set; } = 0; // 납입품 점수
    public int SurvivorScore { get; private set; } = 10; // 생존자 수 점수

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

    // 밤 스킵 처리
    public void SkipNightConfirmed()
    {
        ForceEndNightPhase(); // 강제 밤 종료
    }

    // 외부에서 밤 페이즈 강제 종료 
    public void ForceEndNightPhase()
    {
        if (CurrentPhase == Phase.Night && gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine); // 기존 루프 중단
            NextDay(); // 다음 날로 진행
            CurrentPhase = Phase.Day; // 페이즈를 낮으로
            ApplyGlobalLight(); // 조명 적용
            gameLoopCoroutine = StartCoroutine(GameLoopCoroutine()); // 새로운 루프 시작
        }
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
                    SetItemRatios(0.7f, 0.2f, 0.1f); 
                    break;
                case GameDays.SecondDay: 
                    SetItemRatios(0.5f, 0.3f, 0.2f); 
                    break;
                case GameDays.ThirdDay: 
                    SetItemRatios(0.3f, 0.4f, 0.3f); 
                    break;
                case GameDays.FourthDay: 
                    SetItemRatios(0.2f, 0.3f, 0.5f); 
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
                    SetItemRatios(0.5f, 0.3f, 0.2f); 
                    break;
                case GameDays.SecondDay: 
                    SetItemRatios(0.4f, 0.3f, 0.3f); 
                    break;
                case GameDays.ThirdDay: 
                    SetItemRatios(0.3f, 0.3f, 0.4f); 
                    break;
                case GameDays.FourthDay: 
                    SetItemRatios(0.2f, 0.3f, 0.5f); 
                    break;
            }

            spawnManager.SpawnZombiesOnly(CurrentZombieSpawnCount); // 좀비만 스폰
        } 
        else
        {
            Debug.Log("[GameManager] 쉘터에서는 밤 스폰 생략");
        }
    }
    
    // MARK: 아이템별 스폰 비율 설정
    void SetItemRatios(float heal, float weapon, float lantern)
    {
        if (spawnManager != null && spawnManager.itemInfos != null && spawnManager.itemInfos.Length >= 3)
        {
            spawnManager.itemInfos[0].ratio = heal;
            spawnManager.itemInfos[1].ratio = weapon;
            spawnManager.itemInfos[2].ratio = lantern;
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
