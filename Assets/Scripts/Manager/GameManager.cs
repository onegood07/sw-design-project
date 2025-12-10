using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using Random = UnityEngine.Random;
using UnityEngine.UI; 
using System.Linq; 

// 게임 상태 관련 Enum 정의
public enum GameEnding { None, Happy, GameOver, Bad } 
public enum GameDays { FirstDay, SecondDay, ThirdDay }
public enum Phase { Day, Night } 


public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance;

    // 게임 상태 변수
    public GameEnding CurrentEnding { get; private set; } 
    public GameDays CurrentDay { get; private set; } 
    public Phase CurrentPhase { get; private set; } 

    public bool IsInShelter { get; set; } = false; 
    
    // 타이머 상태 변수 (ClockHUD가 참조)
    public float DayTimer { get; private set; } = 0f; 
    public float NightTimer { get; private set; } = 0f; 
    
    // MARK: 납입품 관련 설정
    [Header("Item Submission Settings")]
    public Item[] AvailableSubmitItems; // Item 클래스가 외부에서 정의되어 있다고 가정합니다.
    
    public Dictionary<Item, int> CurrentRequiredItemsData { get; private set; } = new Dictionary<Item, int>();
    public Dictionary<Item, int> CurrentSubmittedData { get; private set; } = new Dictionary<Item, int>();
    
    // 납입 목표는 이제 '점수'입니다.
    public int TargetRequiredScore = 100; 
    public int MaxRequiredIncrease = 3;

    // MARK: 좀비 능력치 배율 설정
    [Header("Zombie Multipliers")]
    public float NightSpeedMultiplier = 1.3f; 
    public float NightHpMultiplier = 1.2f; 
    public float NightPowerMultiplier = 1.5f; 

    // MARK: 점수 및 생존자 관련
    [Header("Scores & Survivors")]
    public int ShelterItemScore = 0; 
    public int SurvivorScore = 10; 
    public int InitialSurvivorCount = 10;     
    [HideInInspector] public int SurvivorCount; 
    
    // 이전 날의 요구/납입 점수 추적
    private int PreviousDayTargetScore = 0; 
    private int PreviousDaySubmittedScore = 0; 

    // 스폰 관련 설정
    [Header("Spawn Settings")]
    public SpawnManager spawnManager; // SpawnManager 클래스가 외부에서 정의되어 있다고 가정합니다.
    public int ItemSpawnCount = 70;
    public int NPCSpawnCount = 5;
    public int BaseZombieSpawnCount = 30; 
    private int CurrentZombieSpawnCount;

    // 페이즈 지속 시간 (시간 비율 조정: 예시로 60초/120초로 늘림)
    [Header("Phase Duration")]
    public float dayDuration = 2.0f; // 낮 지속 시간
    public float nightDuration = 20.0f; // 밤 지속 시간
    
    // 씬 이름 설정
    [Header("Scene Settings")]
    public string MainWorldSceneName = "Main";
    public string ShelterSceneName = "InsideShelter"; 

    // 게임엔딩 UI
    [Header("GameEnding Settings")]
    public GameObject gameOverPanel; 
    public GameObject uiRoot;
    
    // 일차 변경 UI 설정
    [Header("Day Change UI")]
    public GameObject DayChangePanel;      // 인스펙터에 할당되어야 합니다.
    public Text DayChangeText;             // 인스펙터에 할당되어야 합니다.
    public float DayChangeDisplayTime = 3.0f;
    public float SlideDuration = 0.5f; 

    private Coroutine gameLoopCoroutine; 
    private Coroutine dayChangeCoroutine = null; 

    private RectTransform dayChangeRectTransform; 
    // CanvasGroup 참조
    private CanvasGroup dayChangeCanvasGroup; 
    
    // 초기 중앙 위치 저장용 변수
    private Vector2 initialMidPosition; 

    // 대화창 관련 플래그
    public bool IsDialogueActive { get; private set; } = false;

    // MARK: Awake 함수
    void Awake()
    {
        CurrentEnding = GameEnding.None;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else 
        {
            Destroy(gameObject); 
        }
        
        if (DayChangePanel != null)
        {
            dayChangeRectTransform = DayChangePanel.GetComponent<RectTransform>(); 
            
            if (dayChangeRectTransform != null)
            {
                initialMidPosition = dayChangeRectTransform.anchoredPosition;
            }
            
            dayChangeCanvasGroup = DayChangePanel.GetComponent<CanvasGroup>();
            if (dayChangeCanvasGroup == null)
            {
                 dayChangeCanvasGroup = DayChangePanel.AddComponent<CanvasGroup>();
            }
        }
    }

    IEnumerator Start()
    {
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;

        CurrentZombieSpawnCount = BaseZombieSpawnCount;
        SurvivorCount = InitialSurvivorCount; 

        ApplyGlobalLight(); 
        
        GenerateRequiredItems(); 
        
        if (DayChangePanel != null && DayChangeText != null)
        {
            DayChangeText.text = $"{GetDayString(CurrentDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐.";
            StartCoroutine(ShowDayChangeCoroutine(0)); 
        }
        
        Debug.Log($"☀️ [{CurrentDay}] 낮 시작! (즉시 스폰)");
        
        // Start에서 즉시 스폰 요청 (씬 로드 후 즉시 실행)
        StartDayPhase(true); 
        
        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        
        yield break;
    }
    
    // MARK: 씬 로드 및 스폰 위치 설정을 위한 헬퍼 함수
    public void SetPlayerSpawnAndLoadScene(string sceneName, Vector3 spawnPosition)
    {
        // IsInShelter 상태 업데이트
        if (sceneName == MainWorldSceneName)
        {
            IsInShelter = false; 
        }
        else if (sceneName == ShelterSceneName)
        {
            IsInShelter = true; 
        }

        // ⭐ 수정: FadeManager의 위치 지정 오버로드 함수를 호출합니다.
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOutToScene(sceneName, spawnPosition); 
            Debug.Log($"[GameManager] FadeManager를 통해 씬 전환 요청: {sceneName}, 목표 스폰 위치 전달.");
        }
        else
        {
            Debug.LogError("[GameManager] FadeManager 인스턴스를 찾을 수 없습니다. 페이드 없이 강제 씬 로드.");
            SceneManager.LoadScene(sceneName);
        }
    }

    // MARK: 플레이어 사망 처리
public void PlayerDied()
{
    CurrentEnding = GameEnding.GameOver;
    Debug.Log("[GameManager] 게임 오버 엔딩");

    // 1. 메인 UI 비활성화 (uiRoot는 GameManager에 남아있어도 됨)
    if (uiRoot != null)
        uiRoot.SetActive(false);

    // 2. 🌟 GameOverCanvasManager의 싱글톤 인스턴스를 찾아 패널 활성화
    // 만약 싱글톤이 제대로 작동한다면, 이 호출은 유일한 캔버스에만 적용됩니다.
    if (GameOverCanvasManager.Instance != null)
    {
        // Canvas 자체가 DontDestroyOnLoad 되어 있으므로, SetPanelActive를 호출
        GameOverCanvasManager.Instance.SetPanelActive(true);
    }
    else
    {
         // 🚨 이 로그가 뜬다면 싱글톤 Awake보다 PlayerDied가 먼저 호출된 경우이거나, 스크립트가 없습니다.
         Debug.LogError("[GameManager] GameOverCanvasManager 싱글톤 인스턴스를 찾을 수 없습니다.");
    }

    // 3. 메인 게임 루프 코루틴 중단
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
            return; 
        }

        if (currentScene == MainWorldSceneName)
        {
            // LightController가 외부에서 정의되어 있다고 가정하고 주석 처리
            if (LightController.Instance != null)
            {
                LightController.Instance.UpdateGlobalLight(CurrentPhase); 
            }
            else
            {
                Debug.LogWarning("[GameManager] Main 씬이지만 LightController.Instance를 찾을 수 없습니다.");
            }
           
        }
    }

    // TODO: 일차별 시간대 설정 (반드시 GameLoopCoroutine와 동일하게 수정해줘야함)
    public (float dayTime, float nightTime) GetDurationForDay(GameDays day)
    {
        switch (day)
        {
            case GameDays.FirstDay: 
                    // 1일차: 낮 7분 (420초), 밤 3분 (180초)
                   return (180f, 180f);
                case GameDays.SecondDay: 
                    // 2일차: 낮 6분 (360초), 밤 4분 (240초)
                    return (180f, 180f);
                case GameDays.ThirdDay: 
                    // 3일차: 낮 5분 (300초), 밤 5분 (300초)
                    return (180f, 180f);
                default:
                    return (120f, 180f); // 안전 반환값
        }
    }

 // TODO: 일차별 시간대 설정 (반드시 GetDurationForDay 동일하게 수정해줘야함)
// MARK: 전체 게임 루프 코루틴 (일차별 시간 조정)
    IEnumerator GameLoopCoroutine()
    {
        // ⭐ DayDuration과 NightDuration을 동적으로 가져오는 헬퍼 함수
        (float dayTime, float nightTime) GetDuration(GameDays day)
        {
            switch (day)
            {
                case GameDays.FirstDay: 
                    // 1일차: 낮 7분 (420초), 밤 3분 (180초)
                   return (180f, 180f);
                case GameDays.SecondDay: 
                    // 2일차: 낮 6분 (360초), 밤 4분 (240초)
                    return (180f, 180f);
                case GameDays.ThirdDay: 
                    // 3일차: 낮 5분 (300초), 밤 5분 (300초)
                    return (180f, 180f);
                default:
                    // 혹시 모를 경우를 대비한 기본값
                    return (120f, 180f);
            }
        }
        
        // 1일차 낮 시간 설정 (Start()에서 이미 시작된 루프)
        float firstDayDuration = GetDuration(GameDays.FirstDay).dayTime;
        DayTimer = 0f;
        while (DayTimer < firstDayDuration)
        {
            DayTimer += Time.deltaTime;
            yield return null;
        }
        
        // 2일차부터 3일차까지 루프
        while (CurrentDay <= GameDays.ThirdDay)
        {
            (float currentDayDuration, float currentNightDuration) = GetDuration(CurrentDay);

            // 1. 밤 페이즈
            CurrentPhase = Phase.Night;
            // 좀비 수량 증가 (밤 페이즈 시작 시)
            CurrentZombieSpawnCount += 20; 
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작! ({currentNightDuration/60f:F1}분 = {currentNightDuration:F0}초)");
            ApplyGlobalLight();
            
            // 밤 스폰 요청 (추가 스폰)
            StartNightPhase();
            
            NightTimer = 0f;
            while (NightTimer < currentNightDuration) // ⭐ 동적 밤 시간 적용
            {
                NightTimer += Time.deltaTime;
                yield return null;
            } 

            // 2. 다음 날 전환
            yield return StartCoroutine(NextDayCoroutine()); 
            
            if (CurrentDay > GameDays.ThirdDay)
            {
                CurrentEnding = GameEnding.Happy;
                Debug.Log("[GameManager] 생존 성공! Happy Ending");
                break; 
            } 
            
            // 3. 낮 페이즈
            (currentDayDuration, currentNightDuration) = GetDuration(CurrentDay); // 다음 날 시간 다시 가져옴
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작! ({currentDayDuration/60f:F1}분 = {currentDayDuration:F0}초)");
            ApplyGlobalLight(); 
            
            // 낮 스폰 요청 (초기 스폰 - 씬 오브젝트 초기화 후)
            StartDayPhase(false);    
            
            DayTimer = 0f;
            while (DayTimer < currentDayDuration) // ⭐ 동적 낮 시간 적용
            {
                DayTimer += Time.deltaTime;
                yield return null;
            }
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }

 // MARK: 납입 점수 기준에 따른 생존자 감소 계산
    private int CalculateSurvivorLoss()
    {
        // 납입 점수 계산: Item에 itemDataAsset이 없으므로 임시로 10점씩 부여하는 로직을 따릅니다.
        PreviousDaySubmittedScore = 0;
        foreach (var pair in CurrentSubmittedData)
        {
            // ⭐ 납입품은 임시로 개당 10점으로 가정
            PreviousDaySubmittedScore += pair.Value * 10; 
        }
        
        PreviousDayTargetScore = TargetRequiredScore;
        
        int submittedScore = PreviousDaySubmittedScore;
        int lossCount = 0;

        // 임시로 TargetRequiredScore가 100이라고 가정
        if (submittedScore >= 100) lossCount = 0; 
        else if (submittedScore >= 70) lossCount = 2;
        else if (submittedScore >= 50) lossCount = 3;
        else if (submittedScore >= 40) lossCount = 4;
        else lossCount = 5;
        
        int actualLoss = Mathf.Min(lossCount, SurvivorCount);
        SurvivorCount -= actualLoss;
        
        Debug.Log($"[Survival Check] 납입 점수: {submittedScore} / {PreviousDayTargetScore}. 생존자 {actualLoss}명 감소. 잔여 생존자: {SurvivorCount}");

        if (SurvivorCount <= 0)
        {
             if (CurrentEnding == GameEnding.None)
                PlayerDied(); 
        }

        return actualLoss;
    }

// MARK: 낮 페이즈 로직 (isInitialSpawn: Start()에서 처음 호출될 때 true)
void StartDayPhase(bool isInitialSpawn)
{
    if (IsInShelter) 
    {
        Debug.Log("[GameManager] 쉘터 내부이므로 낮 스폰을 생략합니다.");
        return; 
    }
    
    if (spawnManager != null)
    {
        // 🌟 InitialSpawn이거나 (게임 시작), 씬이 변경된 후 첫 낮 페이즈일 때만 StartSpawnProcess를 호출합니다.
        if (isInitialSpawn || SceneManager.GetActiveScene().name == MainWorldSceneName)
        {
            DynamicSetItemRatios(); 
            // StartSpawnProcess는 Item, NPC, Zombie를 모두 파괴하고 새로 스폰하는 함수라고 가정
            spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
        }
    }
    else
    {
        Debug.LogWarning("[GameManager] SpawnManager가 연결되지 않았습니다. 메인 씬 스폰 생략.");
    }
}

// MARK: 밤 페이즈 로직 
void StartNightPhase()
{
    if (IsInShelter)
    {
        Debug.Log("[GameManager] 쉘터 내부이므로 밤 스폰을 생략합니다.");
        return;
    }

    if (spawnManager != null)
    {
        DynamicSetItemRatios(); 
        // 밤에는 기존 좀비를 유지하고 새로운 좀비만 추가 스폰합니다.
        spawnManager.SpawnZombiesOnly(CurrentZombieSpawnCount); 
    }
    else
    {
        Debug.LogWarning("[GameManager] SpawnManager가 연결되지 않았습니다. 메인 씬 밤 스폰 생략.");
    }
}
    
    // MARK: SpawnManager에 아이템 섹터 비율을 전달하는 함수 (주석 처리)
    void SetItemSectorRatios(float heal, float weapon, float lantern,float quest,float material,float submit)
    {
        if (spawnManager != null)
        {
             spawnManager.ApplyItemSectorRatios(heal, weapon, lantern, quest, material, submit);
             Debug.Log($"[GameManager] 아이템 스폰 비율 설정: Submit={submit:F2}");
        }
    }
    
    // MARK: 아이템 섹터 비율 동적 설정 (납입품 점수 기반 가중치 부여)
    void DynamicSetItemRatios()
    {
        // GetCurrentRequiredTotalScore()를 GetCurrentRequiredTotalScore_Simulated()로 대체
        int requiredScore = GetCurrentRequiredTotalScore_Simulated(); 
        float scoreRatio = Mathf.Clamp01((float)requiredScore / TargetRequiredScore); 
        
        float submitRatio = 0.1f + scoreRatio * 0.3f;
        float baseTotalOtherRatio = 1.0f - submitRatio;
        float baseOthersTotal = 0.8f; 
        
        float healRatio = baseTotalOtherRatio * (0.3f / baseOthersTotal); 
        float weaponRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float lanternRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float questRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float materialRatio = baseTotalOtherRatio * (0.2f / baseOthersTotal);
        
        // SetItemSectorRatios 호출 시 인자 6개를 전달해야 합니다.
        SetItemSectorRatios(healRatio, weaponRatio, lanternRatio, questRatio, materialRatio, submitRatio);
    }

    // MARK: 현재 요구되는 납입품의 총 점수 계산 (시뮬레이션 버전)
    private int GetCurrentRequiredTotalScore_Simulated()
    {
        int requiredScore = 0;
        foreach (var pair in CurrentRequiredItemsData)
        {
             // ItemDataAsset이 없으므로, 요구 수량 * 임시 점수(10점)로 계산
             requiredScore += pair.Value * 10; 
        }
        return requiredScore;
    }

// GameManager.cs 내 GenerateRequiredItems() 함수만 수정합니다.

// MARK: 일차별 랜덤 납입품 목록 생성
    void GenerateRequiredItems()
    {
        CurrentRequiredItemsData.Clear(); 
        CurrentSubmittedData.Clear(); 

        if (AvailableSubmitItems == null || AvailableSubmitItems.Length == 0)
        {
            Debug.LogWarning("[GameManager] AvailableSubmitItems 목록이 비어 있습니다. 납입 요구 생성 불가.");
            return;
        }

        // 💡 수정 시작: 일차별 목표 점수 설정 (가이드라인)
        int calculatedTargetGuide = 100; // 가이드라인 목표 점수
        int scoreFluctuationRange = 10; // 목표 점수 대비 ±10% 변동 허용

        switch (CurrentDay)
        {
            case GameDays.FirstDay:
                calculatedTargetGuide = 100;
                break;
            case GameDays.SecondDay:
                calculatedTargetGuide = 150;
                break;
            case GameDays.ThirdDay:
                calculatedTargetGuide = 200;
                break;
            default:
                calculatedTargetGuide = 100;
                break;
        }
        
        // 최종적으로 분배할 목표 점수를 가이드라인 근처에서 랜덤하게 결정 (예: 100점 기준 90~110점 사이)
        int minTarget = Mathf.Max(1, calculatedTargetGuide - scoreFluctuationRange);
        int maxTarget = calculatedTargetGuide + scoreFluctuationRange;
        int calculatedTargetScore = Random.Range(minTarget, maxTarget + 1);

        // 1. 요구 아이템 종류 최소/최대 설정 (최소 3종, 최대 6종)
        const int MIN_REQUIRED_ITEMS = 3; 
        const int MAX_REQUIRED_ITEMS = 6;
        
        // 2. 가중치 풀 생성 (실제 아이템 점수 사용)
        List<(Item item, int score, float weight)> weightedPool = new List<(Item, int, float)>();
        float totalWeight = 0f;

        foreach (Item item in AvailableSubmitItems)
        {
            // 임시로 3점에서 5점 사이의 랜덤 값을 사용 (실제 데이터에 맞게 수정 필요)
            int score = Random.Range(3, 6); // 3, 4, 5 중 하나

            if (score <= 0) continue; 

            // 점수가 높을수록 가중치를 낮춥니다. 
            // 가중치는 목표 점수가 아닌 100을 기준으로 계산하여 아이템 선택 확률의 일관성을 유지합니다.
            float weight = 100f / (float)score; 
            weightedPool.Add((item, score, weight));
            totalWeight += weight;
        }
        
        int availableUniqueItems = weightedPool.Count;

        // 유효 아이템 개수 검증 (3개 미만 경고 로직 유지)
        if (availableUniqueItems < MIN_REQUIRED_ITEMS)
        {
            Debug.LogWarning($"[GameManager] 유효한 납입 아이템이 {availableUniqueItems}개 밖에 없어 {MIN_REQUIRED_ITEMS}개 이상 요구할 수 없습니다. 가능한 모든 ({availableUniqueItems}개) 아이템을 요구합니다.");
        }
        
        // 실제로 요구할 아이템 종류 개수 결정 로직 유지
        int maxItemsToRequire = Mathf.Min(MAX_REQUIRED_ITEMS, availableUniqueItems);
        int requiredItemCount = Random.Range(Mathf.Min(MIN_REQUIRED_ITEMS, maxItemsToRequire), maxItemsToRequire + 1);

        if (requiredItemCount == 0 && availableUniqueItems > 0) 
        {
            requiredItemCount = 1; 
        }

        if (requiredItemCount == 0) return; // 요구할 아이템이 없으면 종료

        // 3. 가중치 기반 아이템 무작위 선택 로직 유지
        List<(Item item, int score)> selectedItemsWithScore = new List<(Item, int)>();
        
        for (int i = 0; i < requiredItemCount; i++)
        {
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            Item selectedItem = null;
            int selectedScore = 0;
            int selectedIndex = -1;

            for (int j = 0; j < weightedPool.Count; j++)
            {
                currentWeight += weightedPool[j].weight;
                if (randomValue <= currentWeight)
                {
                    selectedItem = weightedPool[j].item;
                    selectedScore = weightedPool[j].score;
                    selectedIndex = j;
                    break;
                }
            }
            
            if (selectedItem == null) break;
            
            selectedItemsWithScore.Add((selectedItem, selectedScore));
            
            totalWeight -= weightedPool[selectedIndex].weight;
            weightedPool.RemoveAt(selectedIndex);
        }
        
        // 4. 점수 할당 및 수량 계산 (calculatedTargetScore를 기준으로 할당)
        int actualSelectedCount = selectedItemsWithScore.Count;
        
        if (actualSelectedCount == 0)
        {
            Debug.LogError("[GameManager] 아이템 풀은 있었으나 무작위 선택 과정에서 실패했습니다. 로직 오류 또는 데이터 문제.");
            return;
        }
        
        int remainingScore = calculatedTargetScore;
        List<int> scoreAllocations = new List<int>();
        
        int minTotalScoreNeeded = actualSelectedCount; 
        if (remainingScore < minTotalScoreNeeded) remainingScore = minTotalScoreNeeded;

        // 남은 점수를 분배 (각 아이템에 최소 1점 할당)
        for (int i = 0; i < actualSelectedCount - 1; i++)
        {
            int minScore = 1;
            int maxAllocation = remainingScore - (actualSelectedCount - (i + 1)); 
            if (maxAllocation < minScore) maxAllocation = minScore;
            
            // ⭐ 수정: 할당 점수 무작위성 범위 확장 (최대 할당 점수의 30%까지 랜덤하게 할당)
            // 이렇게 하면, 각 아이템의 점수 기여도가 더 불규칙해져 최종 합산 점수가 calculatedTargetScore와 더 '랜덤'해집니다.
            int baseAllocation = (int)((float)remainingScore / (actualSelectedCount - i));
            int randomAllocationRange = Mathf.Max(1, baseAllocation / 3);
            
            // 할당 점수의 무작위성을 높입니다.
            int allocatedScore = Random.Range(Mathf.Max(minScore, baseAllocation - randomAllocationRange), Mathf.Min(maxAllocation, baseAllocation + randomAllocationRange) + 1);
            
            scoreAllocations.Add(allocatedScore);
            remainingScore -= allocatedScore;
        }
        scoreAllocations.Add(remainingScore); // 마지막 아이템에 남은 점수 전부 할당
        
        int actualTotalRequiredScore = 0;
        
        // 최종 수량 계산
        for (int i = 0; i < selectedItemsWithScore.Count; i++)
        {
            var (item, itemUnitScore) = selectedItemsWithScore[i];
            int requiredScorePortion = scoreAllocations[i];
            
            int requiredCount = Mathf.CeilToInt((float)requiredScorePortion / itemUnitScore);
            
            CurrentRequiredItemsData.Add(item, requiredCount);
            CurrentSubmittedData.Add(item, 0); 

            actualTotalRequiredScore += requiredCount * itemUnitScore; 
        }
        
        // 💡 수정 완료: TargetRequiredScore를 계산된 실제 요구 총 점수로 업데이트
        TargetRequiredScore = actualTotalRequiredScore;

        Debug.Log($"[GameManager] {GetDayString(CurrentDay)} 납입 요구 목록 생성. (가이드라인: {calculatedTargetGuide}점). 실제 목표 점수: {TargetRequiredScore}. (총 {CurrentRequiredItemsData.Count}종)");
    }
    // MARK: 다음 날로 전환
    IEnumerator NextDayCoroutine()
    {
        GameDays previousDay = CurrentDay; 
        
        // 납입 점수 계산 및 생존자 감소 처리
        int survivorLoss = CalculateSurvivorLoss();
        
        switch (CurrentDay)
        {
           case GameDays.FirstDay: CurrentDay = GameDays.SecondDay; CurrentZombieSpawnCount += 10; break;
            // ⭐ 3일차가 마지막이므로, 2일차 다음은 3일차
            case GameDays.SecondDay: CurrentDay = GameDays.ThirdDay; CurrentZombieSpawnCount += 20; break;
            // ⭐ 3일차 다음은 Enum의 다음 값 (종료 처리)
            case GameDays.ThirdDay: CurrentDay++; break;
        }

        Debug.Log($"다음 날: {CurrentDay}, 좀비 수: {CurrentZombieSpawnCount}");

       if (CurrentDay != previousDay && CurrentDay <= GameDays.ThirdDay)
        {
            if (spawnManager != null)
            {
                // 일차 변경 시 모든 스폰 데이터를 완전히 초기화합니다.
                spawnManager.ResetPersistentData();
                Debug.Log("[GameManager] 일차 변경으로 아이템/NPC/좀비 영구 데이터 및 씬 오브젝트 초기화 완료.");
            }
            
            GenerateRequiredItems(); 
            
            Coroutine uiWait = ShowDayChangeMessage(CurrentDay, survivorLoss); 
        }

        yield break;
    }

    // MARK: 일차 변경 안내문 표시
    public Coroutine ShowDayChangeMessage(GameDays newDay, int lossCount)
    {
        if (DayChangePanel == null || DayChangeText == null) return null;
        
        string message = lossCount > 0 
            ? $"{GetDayString(newDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐. 어제 납입품을 다 못 채워서 {lossCount}명이 사망했어."
            : $"{GetDayString(newDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐. 어제는 수고 많았어. 사망한 생존자는 없어!";
        
        DayChangeText.text = message;
        
        if (dayChangeCoroutine != null) StopCoroutine(dayChangeCoroutine); 
        dayChangeCoroutine = StartCoroutine(ShowDayChangeCoroutine(lossCount));
        
        return dayChangeCoroutine;
    }

    // MARK: 현재 누적 납입 점수 계산 함수
    public int GetCurrentSubmittedTotalScore()
    {
        int submittedScore = 0;
        foreach (var pair in CurrentSubmittedData)
        {
            // 임시 점수 (ItemDataAsset이 없으므로)
            // if (pair.Key != null) submittedScore += pair.Value * pair.Key.itemDataAsset.score; 
            if (pair.Key != null) submittedScore += pair.Value * 10; 
        }
        return submittedScore;
    }

    // MARK: 현재 누적 납입 점수를 기반으로 예정된 생존자 손실 인원수 예측 함수
    public int PredictSurvivorLoss()
    {
        int submittedScore = GetCurrentSubmittedTotalScore();
        int lossCount = 0;

        // 임시로 TargetRequiredScore가 100이라고 가정
        if (submittedScore >= 100) lossCount = 0;
        else if (submittedScore >= 70) lossCount = 2;
        else if (submittedScore >= 50) lossCount = 3;
        else if (submittedScore >= 40) lossCount = 4;
        else lossCount = 5;

        return Mathf.Min(lossCount, SurvivorCount);
    }

    // MARK: 일차 변경 안내 애니메이션 
    private IEnumerator ShowDayChangeCoroutine(int lossCount)
    {
        if (dayChangeRectTransform == null || DayChangePanel == null || dayChangeCanvasGroup == null)
        {
            dayChangeCoroutine = null;
            yield break;
        }
    
        Vector2 midPos = initialMidPosition; 
        Vector2 startPos = new Vector2(midPos.x + 800.0f, midPos.y); 
        Vector2 endPos = new Vector2(midPos.x - 2000f, midPos.y); 
        
        dayChangeCanvasGroup.alpha = 1f;
        DayChangePanel.SetActive(true);

        if (dayChangeRectTransform != null) dayChangeRectTransform.anchoredPosition = startPos;

        float elapsedTime = 0f;
        while (elapsedTime < SlideDuration)
        {
            if (dayChangeRectTransform == null) yield break;
            float t = elapsedTime / SlideDuration;
            dayChangeRectTransform.anchoredPosition = Vector2.Lerp(startPos, midPos, t);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        
        if (dayChangeRectTransform == null) yield break;
        dayChangeRectTransform.anchoredPosition = midPos; 

        yield return new WaitForSeconds(DayChangeDisplayTime);
        
        float fadeOutDuration = SlideDuration;
        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            if (dayChangeRectTransform == null || dayChangeCanvasGroup == null) yield break;
            float t = elapsedTime / fadeOutDuration;
            
            dayChangeRectTransform.anchoredPosition = Vector2.Lerp(midPos, endPos, t);
            dayChangeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        if (DayChangePanel != null)
        {
            DayChangePanel.SetActive(false);
            dayChangeCanvasGroup.alpha = 1f;
        }
        
        dayChangeCoroutine = null; 
    }
    
    private string GetDayString(GameDays day)
    {
        switch (day)
        {
            case GameDays.FirstDay: return "1일차";
            case GameDays.SecondDay: return "2일차";
            case GameDays.ThirdDay: return "3일차";
            default: return "";
        }
    }
    
    // MARK: 씬 로드 시 실행 (UI 참조 복구 로직 강화 및 스폰 복원 총괄)
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsInShelter = (scene.name == ShelterSceneName); 
        
        // SpawnManager 인스턴스를 찾거나 참조를 유지합니다.
        if (spawnManager == null)
        {
             spawnManager = Object.FindFirstObjectByType<SpawnManager>();
        }
        
        if (scene.name == MainWorldSceneName)
        {
            // 🌟 쉘터에서 메인 월드로 돌아왔을 때 좀비 복원 로직을 명시적으로 호출합니다.
            if (spawnManager != null)
            {
                 Debug.Log("[GameManager] 메인 씬 복귀: SpawnManager를 통해 영구/좀비 복원 로직 호출.");
                 
                 // ⭐ [오류 수정] RestoreTemporaryZombiesByCount() 대신 RestorePersistentObjects()를 호출합니다.
                 spawnManager.RestorePersistentObjects(); 
            }

            // 3. UI 참조 복구 로직 (기존 로직 유지)
            if (DayChangePanel == null)
            {
                GameObject newPanelObject = GameObject.Find("DayChangePanel"); 
                
                if (newPanelObject != null)
                {
                    DayChangePanel = newPanelObject;
                    DayChangeText = DayChangePanel.GetComponentInChildren<Text>(); 
                    dayChangeRectTransform = DayChangePanel.GetComponent<RectTransform>(); 
                    dayChangeCanvasGroup = DayChangePanel.GetComponent<CanvasGroup>();
                    
                    if (dayChangeCanvasGroup == null)
                    {
                         dayChangeCanvasGroup = DayChangePanel.AddComponent<CanvasGroup>();
                    }
                    
                    if (dayChangeRectTransform != null)
                    {
                        dayChangeRectTransform.anchoredPosition = initialMidPosition;
                    }
                }
            }

            if (DayChangePanel != null)
            {
                DayChangePanel.SetActive(false);
                if (dayChangeCanvasGroup != null)
                {
                    dayChangeCanvasGroup.alpha = 1f;
                }
            }
        } 
        
        ApplyGlobalLight(); 
    }

    public void ResetGameSession()
    {
        Debug.Log("=========================================");
        Debug.Log("[GameManager] 전체 게임 세션 초기화 시작.");
        
        CurrentEnding = GameEnding.None;
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;
        IsInShelter = false;
        IsDialogueActive = false;
        
        DayTimer = 0f;
        NightTimer = 0f;
        
        CurrentZombieSpawnCount = BaseZombieSpawnCount;
        SurvivorCount = InitialSurvivorCount;
        ShelterItemScore = 0;
        PreviousDayTargetScore = 0; 
        PreviousDaySubmittedScore = 0; 

        CurrentRequiredItemsData.Clear();
        CurrentSubmittedData.Clear();
        
        // SpawnManager 데이터도 초기화
        if (spawnManager != null)
        {
             spawnManager.ResetPersistentData();
        }

        Debug.Log("[GameManager] 전체 게임 세션 초기화 완료.");
        Debug.Log("=========================================");
    }

    public void AddSurvivors(int amount)
    {
        if (amount > 0)
        {
            SurvivorCount += amount; 
            Debug.Log($"[GameManager] 퀘스트 보상으로 생존자 {amount}명이 추가되었습니다. 현재 생존자 수: {SurvivorCount}");
        }
        else
        {
             Debug.LogWarning("[GameManager] AddSurvivors 함수에 0 또는 음수 값이 전달되었습니다. 생존자 수 변경 없음.");
        }
        
        // 💡 필요한 경우 UI 업데이트 로직 호출 (예: HUD 또는 쉘터 UI)
        // if (ShelterUI.Instance != null) ShelterUI.Instance.UpdateSurvivorDisplay(SurvivorCount); 
    }
    // 대화 시작 시 호출
    public void StartInteraction()
    {
        IsDialogueActive = true;
        Debug.Log("[GameManager] 상호작용 시작: 게임 일시 정지 상태.");
    }

    // 대화 종료 시 호출
    public void EndInteraction()
    {
        IsDialogueActive = false;
        Debug.Log("[GameManager] 상호작용 종료: 게임 다시 시작.");
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