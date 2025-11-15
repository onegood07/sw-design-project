using UnityEngine;
using System.Collections;

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
    public float dayDuration = 30f;
    public float nightDuration = 20f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        CurrentZombieSpawnCount = BaseZombieSpawnCount;

        StartCoroutine(GameLoopCoroutine());
    }

    // 전체 게임 루프: 낮 → 밤 → 다음 날
    IEnumerator GameLoopCoroutine()
    {
        while (CurrentDay != GameDays.FourthDay + 1)
        {
            // 낮 페이즈
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");
            StartDayPhase();
            yield return new WaitForSeconds(dayDuration);

            // 밤 페이즈
            CurrentPhase = Phase.Night;
            CurrentZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");
            StartNightPhase();
            yield return new WaitForSeconds(nightDuration);

            // 다음 날 진행
            NextDay();
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }

    // 낮 시작 시 처리
    void StartDayPhase()
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

        // 아이템, NPC, 현재 좀비 수
        spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
    }

    // 밤 시작 시 처리
    void StartNightPhase()
    {
        spawnManager.ClearAll();

        // 밤에는 좀비 중심, 아이템 소폭 조정 가능
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

        // 좀비 수 증가, 아이템은 낮보다 적게
        spawnManager.StartSpawnProcess(Mathf.Max(1, ItemSpawnCount / 2), NPCSpawnCount, CurrentZombieSpawnCount);
    }

    void SetItemRatios(float heal, float weapon, float lantern)
    {
        if (spawnManager.itemInfos.Length >= 3)
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
                CurrentZombieSpawnCount += 10; break;
            case GameDays.ThirdDay: CurrentDay = GameDays.FourthDay; break;
            case GameDays.FourthDay: CurrentDay++; break; // 종료용
        }

        Debug.Log($"다음 날: {CurrentDay}, 좀비 수: {CurrentZombieSpawnCount}");
    }
}
