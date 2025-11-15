using UnityEngine;
using System.Collections;

// 게임 엔딩
public enum GameEnding
{
    Happy,
    GameOver,
    Bad
}

// 게임 일차
public enum GameDays
{
    FirstDay,
    SecondDay,
    ThirdDay,
    FourthDay
}

// 낮과 밤 페이즈
public enum Phase
{
    Day,
    Night
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameDays CurrentDay { get; private set; }
    public GameEnding? Ending { get; private set; } = null;
    public Phase CurrentPhase { get; private set; }
    public int ShelterItemScore { get; private set; } = 0;
    public int SurvivorScore { get; private set; } = 0;

    // 스폰 관련 세팅
    [Header("Spawn Settings")]
    public SpawnManager spawnManager;
    public int ItemSpawnCount = 5;
    public int NPCSpawnCount = 3;
    public int ZombieSpawnCount = 10;

    // 싱글톤 
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
        // 1일차 낮 시작
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        // 처음 스폰
        spawnManager.StartSpawnProcess(
            ItemSpawnCount,
            ZombieSpawnCount,
            NPCSpawnCount
        );

        StartCoroutine(GameProgressCoroutine());
    }

    // 게임 진행 로직
    IEnumerator GameProgressCoroutine()
    {
        while (CurrentDay != GameDays.FourthDay)
        {
            // 낮
            CurrentPhase = Phase.Day;
            Debug.Log($"[☀️ {CurrentDay}] 낮 시작!");

            yield return new WaitForSeconds(30f);

            // 밤
            CurrentPhase = Phase.Night;
            ZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");

            // 기존 객체 모두 삭제
            spawnManager.ClearAll(); 

            spawnManager.StartSpawnProcess(
                ItemSpawnCount,
                ZombieSpawnCount,
                NPCSpawnCount
            );

            yield return new WaitForSeconds(5f);

            // 다음 날
            NextDay();
        }

        Debug.Log("모든 날이 끝났습니다!");
    }

    // 다음 일차 로직
    public void NextDay()
    {
        spawnManager.ClearAll();

        switch (CurrentDay)
        {
            case GameDays.FirstDay:
                CurrentDay = GameDays.SecondDay;
                ZombieSpawnCount += 10;
                break;
            case GameDays.SecondDay:
                CurrentDay = GameDays.ThirdDay;
                ZombieSpawnCount += 10;
                break;
            case GameDays.ThirdDay:
                CurrentDay = GameDays.FourthDay;
                break;
        }
        
        Debug.Log($"[다음 일차] Day: {CurrentDay}, 좀비 수: {ZombieSpawnCount}");

        spawnManager.StartSpawnProcess(
            ItemSpawnCount,
            ZombieSpawnCount,
            NPCSpawnCount
        );
    }
}
