using UnityEngine;

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
    public SpawnManager spawnManager;
    public int ZombieSpawnCount = 10;

    // 싱글톤
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
        
    }

    void Update()
    {
        
    }
}
