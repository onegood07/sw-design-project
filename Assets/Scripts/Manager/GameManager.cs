using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 

public enum GameEnding { None, Happy, GameOver, Bad }
public enum GameDays { FirstDay, SecondDay, ThirdDay, FourthDay }
public enum Phase { Day, Night }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 현재 게임 엔딩 변수
    public GameEnding CurrentEnding { get; private set; }
    // 현재 게임 일차 변수
    public GameDays CurrentDay { get; private set; }
    // 현재 게임 페이즈 변수
    public Phase CurrentPhase { get; private set; }

    // 현재 씬이 쉘터인지 확인
    public bool IsInShelter { get; set; } = false;

    // 점수 관련 (납입품, 생존자수)
    [Header("Scores")]
    public int ShelterItemScore { get; private set; } = 0; // 납입품
    public int SurvivorScore { get; private set; } = 10; // 생존자수

    // 스폰 관련 세팅
    [Header("Spawn Settings")]
    public SpawnManager spawnManager;
    public int ItemSpawnCount = 5;
    public int NPCSpawnCount = 3;
    public int BaseZombieSpawnCount = 10;
    private int CurrentZombieSpawnCount;

    // 페이즈 시간
    [Header("Phase Duration")]
    public float dayDuration = 1.0f;
    public float nightDuration = 20.0f;
    
    // 씬 세팅
    [Header("Scene Settings")]
    public string MainWorldSceneName = "Main";
    public string ShelterSceneName = "InsideShelter"; 

    // 게임엔딩 세팅
    [Header("GameEnding Settings")]
    public GameObject gameOverPanel;
    public GameObject uiRoot;    

    private Coroutine gameLoopCoroutine; 

    void Awake()
    {
        // 시작 시 게임엔딩은 우선 존재하지 않음
        CurrentEnding = GameEnding.None;

        // 싱글톤 선언
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 다음 씬 전환해도 파괴 안됨
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 시작 시 기본 세팅 (1일차, 낮 페이즈)
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        // 현재 좀비 스폰 수를 기존 좀비 스폰 수로 설정
        CurrentZombieSpawnCount = BaseZombieSpawnCount;

        // 조명 반영
        ApplyGlobalLight();

        // 게임 진행 루프 코루틴 동작하기
        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
    }

    // 플레이어 사망 시 실행할 함수 로직
  public void PlayerDied()
    {
        CurrentEnding = GameEnding.GameOver;
        Debug.Log("[GameManager] 게임 오버 엔딩");

        // 기존 UI 비활성화 (체력바, 시간 표기, 허기바, 슬롯 등)
        if (uiRoot != null)
        uiRoot.SetActive(false);

        // 게임 오버 UI 표시
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 게임 루프 멈추기
        if (gameLoopCoroutine != null)
            StopCoroutine(gameLoopCoroutine);
    }

    public void ApplyGlobalLight()
    {
        // 현재 씬 매니저가 액티브한 씬의 이름 파악
        string currentScene = SceneManager.GetActiveScene().name;

        // 현재 씬 이름이 쉘터라면
        if (currentScene == ShelterSceneName)
        {
            // 조명 변경 X
            Debug.Log($"[GameManager] {currentScene} 씬은 쉘터이므로 조명을 변경하지 않습니다.");
            return;
        }

        // 현재 씬 이름이 메인이라면
        if (currentScene == MainWorldSceneName)
        {
            // 현재 페이즈값 반영하여 조명 업데이트
            LightController.Instance?.UpdateGlobalLight(CurrentPhase); 
        }
        else
        {
            Debug.Log($"[GameManager] {currentScene} 씬은 월드가 아니므로 조명을 변경하지 않습니다.");
        }
    }

    // 전체 게임 루프
    IEnumerator GameLoopCoroutine()
    {
        // 마지막 날(=4일차)가 될 때까지 지속
        while (CurrentDay <= GameDays.FourthDay)
        {
            // 낮 페이즈
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");

            // 낮페이즈 - 조명 반영
            ApplyGlobalLight();

             // 낮페이즈 - 시작
            StartDayPhase(); 
            // 밤페이즈가 올 때까지 대기
            yield return new WaitForSeconds(dayDuration);

            // 밤 페이즈
            CurrentPhase = Phase.Night;
            // 밤이 오면 좀비 스폰 수 증가
            CurrentZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");

            // 밤페이즈 - 조명 반영
            ApplyGlobalLight();

            // 밤페이즈 로직 시작
            StartNightPhase(); 
            yield return new WaitForSeconds(nightDuration);

            // 밤이 끝나면 다음 날 진행
            NextDay();
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }

    // 쉘터 내부의 침대와 상호작용했을 때 스킵하시겠습니까?에 yes 버튼을 눌렀을 때 동작
    public void SkipNightConfirmed()
    {
        // 밤페이즈 스킵
        ForceEndNightPhase();
    }


    // 외부에서 밤 페이즈를 강제 종료하고 다음 날로 넘어가는 함수
    public void ForceEndNightPhase()
    {
        if (CurrentPhase == Phase.Night && gameLoopCoroutine != null)
        {
            // 현재 실행 중인 GameLoopCoroutine을 중단
            StopCoroutine(gameLoopCoroutine);
            
            // 다음 날로 전환
            NextDay();
            
            // 조명을 낮으로 바로 전환
            CurrentPhase = Phase.Day;
            ApplyGlobalLight();
            
            // 새로운 루프 시작
            gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        }
    }

    // 낮 시작 시 처리
    void StartDayPhase()
    {
        // 쉘터 내부가 아니라면
        if (!IsInShelter)   
        {
            // 모든 스폰 오브젝트 초기화
            spawnManager.ClearAll();

            // 일차별 낮 아이템 비율 조정
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

            // 아이템, NPC, 현재 좀비 수에 맞게 스폰 시작
            spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
        } 
        else
        {
           Debug.Log("[GameManager] 쉘터에서는 낮 스폰 생략");
        }
    }

    // 밤 시작 시 처리
    void StartNightPhase()
    {
        // 쉘터 내부가 아니라면
        if (!IsInShelter)
        {
             // 일차별 밤 아이템 비율 조정
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

            // 좀비만 추가로 스폰하기
            spawnManager.SpawnZombiesOnly(CurrentZombieSpawnCount);
        } else
        {
            Debug.Log("[GameManager] 쉘터에서는 밤 스폰 생략");
        }
    }
    
    // 아이템별 스폰 비율 조정 함수
    void SetItemRatios(float heal, float weapon, float lantern)
    {
        // itemInfos 배열이 null이 아니고 충분한 크기인지 확인
        if (spawnManager != null && spawnManager.itemInfos != null && spawnManager.itemInfos.Length >= 3)
        {
            spawnManager.itemInfos[0].ratio = heal;
            spawnManager.itemInfos[1].ratio = weapon;
            spawnManager.itemInfos[2].ratio = lantern;
        }
    }

    // 다음 일차 변경 함수 (+ 좀비 수 변경까지 담당)
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

    // 씬이 로드될 때 호출되는 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 쉘터 내부라면
        if (scene.name == ShelterSceneName)
            IsInShelter = true; // true로 변경
        else
            IsInShelter = false; // 아니면 false

        // main 씬이라면
        if (scene.name == MainWorldSceneName)
            spawnManager = Object.FindFirstObjectByType<SpawnManager>(); // 스폰매니저 찾아서 연결

        ApplyGlobalLight();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}
