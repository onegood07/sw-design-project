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
    
    // ⭐ 제거됨: [HideInInspector] public Vector3 SpawnPositionAfterLoad = Vector3.zero;

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
    public int ItemSpawnCount = 5;
    public int NPCSpawnCount = 3;
    public int BaseZombieSpawnCount = 10; 
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
        StartDayPhase(); 
        
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

        if (uiRoot != null)
            uiRoot.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

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

    // MARK: 전체 게임 루프 코루틴
  // MARK: 전체 게임 루프 코루틴 (일차별 시간 조정)
IEnumerator GameLoopCoroutine()
{
    // ⭐ DayDuration과 NightDuration을 동적으로 가져오는 헬퍼 함수
    (float dayTime, float nightTime) GetDuration(GameDays day)
    {
        // 💡 모든 일차의 낮/밤 시간을 테스트용으로 30초(0.5분)로 고정합니다.
        const float TEST_DURATION = 30f; 

        switch (day)
        {
           case GameDays.FirstDay: 
                    return (TEST_DURATION, TEST_DURATION); // 1일차: 낮 30초, 밤 30초
                case GameDays.SecondDay: 
                    return (TEST_DURATION, TEST_DURATION); // 2일차: 낮 30초, 밤 30초
                case GameDays.ThirdDay: 
                    return (TEST_DURATION, TEST_DURATION); // 3일차: 낮 30초, 밤 30초
                default:
                    // 혹시 모를 경우를 대비한 기본값
                    return (TEST_DURATION, TEST_DURATION);
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
        CurrentZombieSpawnCount += 20; 
        Debug.Log($"🌙 [{CurrentDay}] 밤 시작! ({currentNightDuration/60f:F1}분 = {currentNightDuration:F0}초)");
        ApplyGlobalLight();
        StartNightPhase();
        
        NightTimer = 0f;
        while (NightTimer < currentNightDuration) // ⭐ 동적 밤 시간 적용 (30초)
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
        StartDayPhase();    
        
        DayTimer = 0f;
        while (DayTimer < currentDayDuration) // ⭐ 동적 낮 시간 적용 (30초)
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
        if (PreviousDayTargetScore <= 0) return 0; 

        int submittedScore = PreviousDaySubmittedScore;
        int lossCount = 0;

        if (submittedScore >= 100) lossCount = 0; 
        else if (submittedScore >= 70) lossCount = 2;
        else if (submittedScore >= 50) lossCount = 3;
        else if (submittedScore >= 40) lossCount = 4;
        else lossCount = 5;
        
        int actualLoss = Mathf.Min(lossCount, SurvivorCount);
        SurvivorCount -= actualLoss;
        
        Debug.Log($"[Survival Check] 납입 점수: {submittedScore} / {PreviousDayTargetScore}. 생존자 {actualLoss}명 감소. 잔여 생존자: {SurvivorCount}");

        // TODO: 수정해야함
        // if (SurvivorCount <= 0)
        // {
        //      if (CurrentEnding == GameEnding.None)
        //         PlayerDied(); 
        // }

        return actualLoss;
    }

// MARK: 낮 페이즈 로직
void StartDayPhase()
{
    if (IsInShelter) 
    {
        Debug.Log("[GameManager] 쉘터 내부이므로 낮 스폰을 생략합니다.");
        return; 
    }
    
    if (spawnManager != null)
    {
        DynamicSetItemRatios(); 
        spawnManager.StartSpawnProcess(ItemSpawnCount, NPCSpawnCount, CurrentZombieSpawnCount);
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
             // spawnManager.ApplyItemSectorRatios(heal, weapon, lantern, quest, material, submit);
             Debug.Log($"[GameManager] 아이템 스폰 비율 설정: Submit={submit:F2}");
        }
    }
    
    // MARK: 아이템 섹터 비율 동적 설정 (납입품 점수 기반 가중치 부여)
    void DynamicSetItemRatios()
    {
        int requiredScore = GetCurrentRequiredTotalScore();
        float scoreRatio = Mathf.Clamp01((float)requiredScore / TargetRequiredScore); 
        
        float submitRatio = 0.1f + scoreRatio * 0.3f;
        float baseTotalOtherRatio = 1.0f - submitRatio;
        float baseOthersTotal = 0.8f; 
        
        float healRatio = baseTotalOtherRatio * (0.3f / baseOthersTotal); 
        float weaponRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float lanternRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float questRatio = baseTotalOtherRatio * (0.1f / baseOthersTotal);
        float materialRatio = baseTotalOtherRatio * (0.2f / baseOthersTotal);
        
        SetItemSectorRatios(healRatio, weaponRatio, lanternRatio, questRatio, materialRatio, submitRatio);
    }

    // MARK: 현재 요구되는 납입품의 총 점수 계산
    private int GetCurrentRequiredTotalScore()
    {
        int requiredScore = 0;
        foreach (var pair in CurrentRequiredItemsData)
        {
            // ItemDataAsset이 있다고 가정하고 점수 계산 로직은 주석 처리
            if (pair.Key != null && pair.Key.itemDataAsset != null)
            {
                requiredScore += pair.Value * pair.Key.itemDataAsset.getScore;
            }
        }
        return requiredScore;
    }

    // MARK: 일차별 랜덤 납입품 목록 생성 로직 (주석 처리된 임시 로직 유지)
    void GenerateRequiredItems()
    {
        CurrentRequiredItemsData.Clear(); 
        CurrentSubmittedData.Clear(); 

        if (AvailableSubmitItems == null || AvailableSubmitItems.Length == 0)
        {
            Debug.LogWarning("[GameManager] AvailableSubmitItems 목록이 비어 있습니다. 납입 요구 생성 불가.");
            return;
        }

        int currentTargetScore = TargetRequiredScore; 
        int maxItemsToRequire = Mathf.Min(3, AvailableSubmitItems.Length);
        int requiredItemCount = Random.Range(1, maxItemsToRequire + 1); 
        
        List<(Item item, float weight)> weightedPool = new List<(Item, float)>();
        float totalWeight = 0f;

        foreach (Item item in AvailableSubmitItems)
        {
            int score = 10; // 테스트를 위해 임시 점수 부여
            if (score <= 0) continue; 

            float weight = 100f / (float)score; 
            weightedPool.Add((item, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning("[GameManager] 유효한 점수를 가진 납입 아이템이 없습니다.");
            return;
        }
        
        List<Item> selectedItems = new List<Item>();
        for (int i = 0; i < requiredItemCount; i++)
        {
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            Item selectedItem = null;
            int selectedIndex = -1;

            for (int j = 0; j < weightedPool.Count; j++)
            {
                currentWeight += weightedPool[j].weight;
                if (randomValue <= currentWeight)
                {
                    selectedItem = weightedPool[j].item;
                    selectedIndex = j;
                    break;
                }
            }
            
            if (selectedItem != null)
            {
                selectedItems.Add(selectedItem);
                totalWeight -= weightedPool[selectedIndex].weight;
                weightedPool.RemoveAt(selectedIndex);
                if (weightedPool.Count == 0 || totalWeight <= 0.001f) break;
            }
        }
        
        int remainingScore = currentTargetScore;
        List<int> scoreAllocations = new List<int>();
        
        for (int i = 0; i < selectedItems.Count - 1; i++)
        {
            int minScore = 1;
            int maxAllocation = remainingScore - (selectedItems.Count - (i + 1)); 
            if (maxAllocation < minScore) maxAllocation = minScore;
            
            int allocatedScore = Random.Range(minScore, maxAllocation + 1);
            scoreAllocations.Add(allocatedScore);
            remainingScore -= allocatedScore;
        }
        scoreAllocations.Add(remainingScore); 
        
        int actualTotalRequiredScore = 0;

        for (int i = 0; i < selectedItems.Count; i++)
        {
            Item item = selectedItems[i];
            int itemUnitScore = 10; // 테스트를 위해 임시 점수 사용
            int requiredScorePortion = scoreAllocations[i];
            
            int requiredCount = Mathf.CeilToInt((float)requiredScorePortion / itemUnitScore);
            
            CurrentRequiredItemsData.Add(item, requiredCount);
            CurrentSubmittedData.Add(item, 0); 

            actualTotalRequiredScore += requiredCount * itemUnitScore;
        }

        Debug.Log($"[GameManager] {GetDayString(CurrentDay)} 납입 요구 목록 생성. 목표 점수: {currentTargetScore}, 실제 요구 점수: {actualTotalRequiredScore}");
    }

    // MARK: 다음 날로 전환
    IEnumerator NextDayCoroutine()
    {
        GameDays previousDay = CurrentDay; 
        
        PreviousDayTargetScore = TargetRequiredScore;
        
        PreviousDaySubmittedScore = 0;
        foreach (var pair in CurrentSubmittedData)
        {
            Item item = pair.Key;
            int submittedCount = pair.Value;
            
            if (item != null)
            {
                PreviousDaySubmittedScore += submittedCount * 10;
            }
        }
        
        int survivorLoss = CalculateSurvivorLoss();
        
        switch (CurrentDay)
        {
           case GameDays.FirstDay: CurrentDay = GameDays.SecondDay; CurrentZombieSpawnCount += 10; break;
            // ⭐ 3일차가 마지막이므로, 2일차 다음은 3일차
            case GameDays.SecondDay: CurrentDay = GameDays.ThirdDay; CurrentZombieSpawnCount += 10; break;
            // ⭐ 3일차 다음은 Enum의 다음 값 (종료 처리)
            case GameDays.ThirdDay: CurrentDay++; break;
            // case GameDays.FourthDay: 제거
        }

        Debug.Log($"다음 날: {CurrentDay}, 좀비 수: {CurrentZombieSpawnCount}");

       if (CurrentDay != previousDay && CurrentDay <= GameDays.ThirdDay)
        {
            if (spawnManager != null)
            {
                spawnManager.ResetPersistentData();
                Debug.Log("[GameManager] 일차 변경으로 아이템/NPC/좀비 영구 데이터 및 씬 오브젝트 초기화 완료.");
            }
            
            GenerateRequiredItems(); 
            
            Coroutine uiWait = ShowDayChangeMessage(CurrentDay, survivorLoss); 
            // if (uiWait != null)
            // {
            //     yield return uiWait;
            // }
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
        if (pair.Key != null) submittedScore += pair.Value * 10; // 임시 점수
    }
    return submittedScore;
}

// MARK: 현재 누적 납입 점수를 기반으로 예정된 생존자 손실 인원수 예측 함수
public int PredictSurvivorLoss()
{
    int submittedScore = GetCurrentSubmittedTotalScore();
    int lossCount = 0;

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
    
    // MARK: 씬 로드 시 실행 (UI 참조 복구 로직 강화)
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsInShelter = (scene.name == ShelterSceneName); 

        if (scene.name == MainWorldSceneName)
        {
            spawnManager = Object.FindFirstObjectByType<SpawnManager>();
            
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
        else 
        {
            spawnManager = null;
        }

        ApplyGlobalLight(); 
    }

    public (float dayTime, float nightTime) GetDurationForDay(GameDays day)
    {
        // ⭐ 테스트를 위해 GameLoopCoroutine 내부의 로직과 동일하게 30초로 변경해야 합니다.
        const float TEST_DURATION = 30f; 

        switch (day)
        {
            case GameDays.FirstDay: 
                    return (TEST_DURATION, TEST_DURATION); // 1일차: 낮 30초, 밤 30초
                case GameDays.SecondDay: 
                    return (TEST_DURATION, TEST_DURATION); // 2일차: 낮 30초, 밤 30초
                case GameDays.ThirdDay: 
                    return (TEST_DURATION, TEST_DURATION); // 3일차: 낮 30초, 밤 30초
                default:
                    return (TEST_DURATION, TEST_DURATION); // 안전 반환값
        }
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
        
        // ⭐ 제거됨: SpawnPositionAfterLoad = Vector3.zero;
        
        CurrentZombieSpawnCount = BaseZombieSpawnCount;
        SurvivorCount = InitialSurvivorCount;
        ShelterItemScore = 0;
        PreviousDayTargetScore = 0; 
        PreviousDaySubmittedScore = 0; 

        CurrentRequiredItemsData.Clear();
        CurrentSubmittedData.Clear();
        
        Debug.Log("[GameManager] 전체 게임 세션 초기화 완료.");
        Debug.Log("=========================================");
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