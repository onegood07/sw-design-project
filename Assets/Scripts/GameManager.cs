using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 

public enum GameEnding { Happy, GameOver, Bad }
public enum GameDays { FirstDay, SecondDay, ThirdDay, FourthDay }
public enum Phase { Day, Night }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameDays CurrentDay { get; private set; }
    public Phase CurrentPhase { get; private set; }

    public bool IsInShelter { get; set; } = false;


    // 점수 관련 (납입품, 생존자수)
    [Header("Scores")]
    public int ShelterItemScore { get; private set; } = 0;
    public int SurvivorScore { get; private set; } = 10;

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

    private Coroutine gameLoopCoroutine; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        CurrentZombieSpawnCount = BaseZombieSpawnCount;

        ApplyGlobalLight();

        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
    }

    public void ApplyGlobalLight()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == ShelterSceneName)
        {
            Debug.Log($"[GameManager] {currentScene} 씬은 쉘터이므로 조명을 변경하지 않습니다.");
            return;
        }

        if (currentScene == MainWorldSceneName)
        {
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
        while (CurrentDay <= GameDays.FourthDay)
        {
            // 낮 페이즈
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");

            ApplyGlobalLight();

            StartDayPhase(); 
            yield return new WaitForSeconds(dayDuration);

            // 밤 페이즈
            CurrentPhase = Phase.Night;
            CurrentZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");

            ApplyGlobalLight();
    
            StartNightPhase(); 
            yield return new WaitForSeconds(nightDuration);

            // 밤이 끝나면 다음 날 진행
            NextDay();
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }

    public void SkipNightConfirmed()
    {
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
            
            // 조명을 낮으로 바로 전환하고 새로운 루프 시작
            CurrentPhase = Phase.Day;
            
            ApplyGlobalLight();
            
            gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        }
    }

    // 낮 시작 시 처리
    void StartDayPhase()
    {
        if (!IsInShelter)   
        {
            spawnManager.ClearAll();

            // 일차별 낮 아이템 비율
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

            // 아이템, NPC, 현재 좀비 수 스폰 시작
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
        if (!IsInShelter)
        {
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ShelterSceneName)
            IsInShelter = true;
        else
            IsInShelter = false;

        if (scene.name == MainWorldSceneName)
            spawnManager = Object.FindFirstObjectByType<SpawnManager>();

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