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
    // 싱글톤
    public static GameManager Instance;

    // 게임 진행 일수 (외부에서 참조 가능, 수정 불가능)
    public GameDays CurrentDay { get; private set; }
    // 게임 엔딩 상태 (외부에서 참조 가능, 수정 불가능)
    public GameEnding? Ending { get; private set; } = null;
    // 현재 페이즈
    public Phase CurrentPhase { get; private set; }
    // 납입 스코어
    public int ShelterItemScore { get; private set; } = 0;
    // 생존자수 스코어
    public int SurvivorScore { get; private set; } = 0;

    [Header("Settings")]
    // 스폰 매니저
    public SpawnManager spawnManager;
    // 좀비 스폰 수
    public int ZombieSpawnCount = 10;

    // 싱글톤 선언
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 이동 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

   void Start()
    {
        // 첫째날 및 낮 페이즈로 설정
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        // 처음 좀비 스폰
        spawnManager.SpawnZombies(ZombieSpawnCount);

        // 일차 자동 진행
        StartCoroutine(GameProgressCoroutine());
    }

    // 코루틴으로 시간마다 자동으로 일차 진행
    IEnumerator GameProgressCoroutine()
    {
        // 3일차까지 진행
        while (CurrentDay != GameDays.FourthDay)
        {
            // 낮
            CurrentPhase = Phase.Day;
            Debug.Log($"[☀️ {CurrentDay}] 낮 시작! 좀비 수: {ZombieSpawnCount}");
            // 5초마다 페이즈 변경
            yield return new WaitForSeconds(5f);

            // 밤
            CurrentPhase = Phase.Night;
            ZombieSpawnCount += 20;
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작! 좀비 수: {ZombieSpawnCount}");
            // 모든 좀비 파괴 후 재생성
            spawnManager.ClearZombies();
            spawnManager.SpawnZombies(ZombieSpawnCount);
            yield return new WaitForSeconds(5f);

            // 다음 일차로 전환
            NextDay();
        }

        // 마지막 날(4일차)인 경우  
        Debug.Log("모든 날이 끝났습니다!");
    }

    void Update()
    {

    }

    // 다음 일차로 변경 시 로직 관련 함수
    public void NextDay()
    {
        // 기존 좀비 모두 삭제
        spawnManager.ClearZombies();

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

        // 새로운 좀비 스폰
        spawnManager.SpawnZombies(ZombieSpawnCount);
    }
}
