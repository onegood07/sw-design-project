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

    [Header("Scores")]
    public int ShelterItemScore { get; private set; } = 0;
    public int SurvivorScore { get; private set; } = 10;

    [Header("Spawn Settings")]
    public SpawnManager spawnManager;
    public int ItemSpawnCount = 5;
    public int NPCSpawnCount = 3;
    public int BaseZombieSpawnCount = 10;
    private int CurrentZombieSpawnCount;

    [Header("Phase Duration")]
    public float dayDuration = 1.0f;
    public float nightDuration = 20.0f;
    
    [Header("Scene Settings")]
    public string MainWorldSceneName = "Main"; 

    private Coroutine gameLoopCoroutine; 

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
        }
    }

    void Start()
    {
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        CurrentZombieSpawnCount = BaseZombieSpawnCount;

        if (LightController.Instance != null)
        {
            LightController.Instance.UpdateGlobalLight(CurrentPhase);
        }

        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
    }

    // 전체 게임 루프
    IEnumerator GameLoopCoroutine()
    {
        while (CurrentDay <= GameDays.FourthDay)
        {
            // 낮 페이즈
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");

            LightController.Instance?.UpdateGlobalLight(CurrentPhase);

            StartDayPhase(); 
            yield return new WaitForSeconds(dayDuration);

            // 밤 페이즈
            CurrentPhase = Phase.Night;
            CurrentZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");

            LightController.Instance?.UpdateGlobalLight(CurrentPhase);
    
            StartNightPhase(); 
            yield return new WaitForSeconds(nightDuration);

            // 밤이 끝나면 다음 날 진행
            NextDay();
        }

        Debug.Log("모든 날이 종료되었습니다!");
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
            LightController.Instance?.UpdateGlobalLight(CurrentPhase);
            
            Debug.Log("💤 밤 스킵 완료! 다음 날 낮 시작!");
            gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        }
    }

    // 낮 시작 시 처리
    void StartDayPhase()
    {
        if (SceneManager.GetActiveScene().name == MainWorldSceneName)
        {
            spawnManager.ClearAll();

            // 일차별 낮 아이템 비율
            switch (CurrentDay)
            {
                case GameDays.FirstDay: SetItemRatios(0.7f, 0.2f, 0.1f); break;
                case GameDays.SecondDay: SetItemRatios(0.5f, 0.3f, 0.2f); break;
                case GameDays.ThirdDay: SetItemRatios(0.3f, 0.4f, 0.3f); break;
                case GameDays.FourthDay: SetItemRatios(0.2f, 0.3f, 0.5f); break;
            }

            // 아이템, NPC, 현재 좀비 수 스폰 시작
            spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
        } 
        else
        {
            Debug.Log($"[GameManager] 현재 씬 ({SceneManager.GetActiveScene().name})은 월드 씬이 아니므로 스폰을 건너뜁니다.");
        }
    }

    // 밤 시작 시 처리
    void StartNightPhase()
    {
        if (SceneManager.GetActiveScene().name == MainWorldSceneName)
        {
            spawnManager.ClearAll();

            // 밤에는 좀비 중심, 아이템 소폭 조정 가능
            switch (CurrentDay)
            {
                case GameDays.FirstDay: SetItemRatios(0.5f, 0.3f, 0.2f); break;
                case GameDays.SecondDay: SetItemRatios(0.4f, 0.3f, 0.3f); break;
                case GameDays.ThirdDay: SetItemRatios(0.3f, 0.3f, 0.4f); break;
                case GameDays.FourthDay: SetItemRatios(0.2f, 0.3f, 0.5f); break;
            }

            // 좀비 수 증가, 아이템은 낮보다 적게 스폰 시작
            spawnManager.StartSpawnProcess(Mathf.Max(1, ItemSpawnCount / 2), NPCSpawnCount, CurrentZombieSpawnCount);
        }
        else
        {
            // 쉘터 내부에서는 좀비가 스폰되지 않고 조명만 조정
            Debug.Log($"[GameManager] 현재 씬 ({SceneManager.GetActiveScene().name})은 월드 씬이 아니므로 밤 스폰을 건너뛰고 쉘터 내부 조명만 조정합니다.");
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
}