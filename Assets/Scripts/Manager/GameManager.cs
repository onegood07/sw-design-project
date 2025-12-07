using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using Random = UnityEngine.Random;
using UnityEngine.UI; 
using System.Linq; 

// 게임 상태 관련 Enum 정의
public enum GameEnding { None, Happy, GameOver, Bad } 
public enum GameDays { FirstDay, SecondDay, ThirdDay, FourthDay } 
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

    // MARK: 납입품 관련 설정
    [Header("Item Submission Settings")]
    public Item[] AvailableSubmitItems; 
    
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
    public SpawnManager spawnManager; 
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
    
    // ⭐ 추가: 초기 중앙 위치 저장용 변수
    private Vector2 initialMidPosition; 

    // 대화창 관련 플래그
    public bool IsDialogueActive { get; private set; } = false;

    // MARK: Awake 함수 (UI 초기화 로직 수정)
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
        
        // ⭐ 수정 1: Awake에서 SetActive(false) 제거. 씬 로드 시에 처리합니다.
        // DayChangePanel이 DontDestroyOnLoad 되지 않기 때문에, 첫 씬 로드 시의 참조만 잡아둡니다.
        if (DayChangePanel != null)
        {
            // RectTransform 초기화 및 저장
            dayChangeRectTransform = DayChangePanel.GetComponent<RectTransform>(); 
            
            // ⭐ 추가: 첫 씬 로드 시의 중앙 위치를 저장합니다.
            if (dayChangeRectTransform != null)
            {
                initialMidPosition = dayChangeRectTransform.anchoredPosition;
            }
            
            // CanvasGroup 초기화 및 저장 (없으면 추가)
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
        
        // 납입 요구 목록 생성 (100점 목표)
        GenerateRequiredItems(); 
        
        // 1일차 시작 메시지
        if (DayChangePanel != null && DayChangeText != null)
        {
            DayChangeText.text = $"{GetDayString(CurrentDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐.";
            
            // UI 코루틴을 yield return 없이 시작
            StartCoroutine(ShowDayChangeCoroutine(0)); 
        }
        
        // 1일차 낮 스폰을 즉시 실행
        Debug.Log($"☀️ [{CurrentDay}] 낮 시작! (즉시 스폰)");
        StartDayPhase(); 
        
        // UI 대기 없이 게임 루프 시작
        gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        
        yield break;
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
            // LightController가 외부에서 정의되어 있다고 가정합니다.
         
            // LightController.Instance는 GameManager와 별도로 구현되어야 합니다.
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
    IEnumerator GameLoopCoroutine()
    {
        // 1일차 낮이 시작된 직후부터 루프 시작 (1일차 낮 지속 시간을 기다리는 것부터 시작)
        yield return new WaitForSeconds(dayDuration);
        
        // 1일차 밤부터 시작하여 2일차, 3일차, 4일차까지 낮/밤 반복
        while (CurrentDay <= GameDays.FourthDay)
        {
            // 1. 밤 페이즈 시작
            CurrentPhase = Phase.Night;
            CurrentZombieSpawnCount += 20; 
            Debug.Log($"🌙 [{CurrentDay}] 밤 시작!");
            ApplyGlobalLight();
            StartNightPhase();
            yield return new WaitForSeconds(nightDuration); 

            // 2. 다음 날로 전환 (점수 계산 및 날짜 업데이트)
            NextDay(); 
            
            // 4일차 종료 시 루프 종료
            if (CurrentDay > GameDays.FourthDay) break; 
            
            // 3. 다음 날의 낮 페이즈 시작
            CurrentPhase = Phase.Day;
            Debug.Log($"☀️ [{CurrentDay}] 낮 시작!");
            ApplyGlobalLight(); 
            StartDayPhase();    
            
            // 4. 낮 페이즈 지속 시간 동안 대기 (낮 타이머 시작)
            yield return new WaitForSeconds(dayDuration); 
        }

        Debug.Log("모든 날이 종료되었습니다!");
    }
    
    // MARK: 납입 점수 기준에 따른 생존자 감소 계산
    private int CalculateSurvivorLoss()
    {
        // TargetScore(100)를 기준으로 계산
        if (PreviousDayTargetScore <= 0) return 0; 

        int submittedScore = PreviousDaySubmittedScore;
        int lossCount = 0;

        if (submittedScore >= 100)
        {
            lossCount = 0; // 100점 이상 달성: 0명 감소
        }
        else if (submittedScore >= 70)
        {
            lossCount = 2; // 70점 이상 (100점 미만): 2명 감소
        }
        else if (submittedScore >= 50)
        {
            lossCount = 3; // 50점 이상 (70점 미만): 3명 감소
        }
        else if (submittedScore >= 40)
        {
            lossCount = 4; // 40점 이상 (50점 미만): 4명 감소
        }
        else 
        {
            lossCount = 5; // 40점 미만: 5명 감소
        }
        
        // 생존자 감소 반영
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
        // 동적으로 스폰 비율 설정 (납입품 점수 기반)
        DynamicSetItemRatios(); 
        
        // 복원 데이터가 없으면 초기 스폰을 시도합니다.
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
        // 동적으로 스폰 비율 설정 (납입품 점수 기반)
        DynamicSetItemRatios(); 

        spawnManager.SpawnZombiesOnly(CurrentZombieSpawnCount); 
    }
    else
    {
        Debug.LogWarning("[GameManager] SpawnManager가 연결되지 않았습니다. 메인 씬 밤 스폰 생략.");
    }
}
    
    // MARK: SpawnManager에 아이템 섹터 비율을 전달하는 함수
    void SetItemSectorRatios(float heal, float weapon, float lantern,float quest,float material,float submit)
    {
        if (spawnManager != null)
        {
            // SpawnManager의 ApplyItemSectorRatios 함수로 비율 전달 및 개별 아이템 비율 업데이트
             spawnManager.ApplyItemSectorRatios(heal, weapon, lantern, quest, material, submit);
        }
    }
    
    // MARK: 아이템 섹터 비율 동적 설정 (납입품 점수 기반 가중치 부여)
    void DynamicSetItemRatios()
    {
        int requiredScore = GetCurrentRequiredTotalScore();
        float scoreRatio = Mathf.Clamp01((float)requiredScore / TargetRequiredScore); 
        
        // 납입품 점수에 따라 0.1f에서 최대 0.4f까지 스폰 비율 증가
        float submitRatio = 0.1f + scoreRatio * 0.3f;
        
        float baseTotalOtherRatio = 1.0f - submitRatio;

        // 나머지 섹터 (Heal, Weapon, Lantern, Quest, Material)의 기본 비율 합계 (0.8f)
        float baseOthersTotal = 0.8f; 
        
        // 나머지 섹터 비율 재분배 (0.3/0.1/0.1/0.1/0.2 기준)
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
            if (pair.Key != null && pair.Key.itemDataAsset != null)
            {
                requiredScore += pair.Value * pair.Key.itemDataAsset.getScore;
            }
        }
        return requiredScore;
    }

    // MARK: 일차별 랜덤 납입품 목록 생성 로직 
    void GenerateRequiredItems()
    {
        CurrentRequiredItemsData.Clear(); 
        CurrentSubmittedData.Clear(); 

        if (AvailableSubmitItems == null || AvailableSubmitItems.Length == 0)
        {
            Debug.LogWarning("[GameManager] AvailableSubmitItems 목록이 비어 있습니다. 납입 요구 생성 불가.");
            return;
        }

        // 1. 목표 점수 및 요구 아이템 개수 결정
        int currentTargetScore = TargetRequiredScore; 
        int maxItemsToRequire = Mathf.Min(3, AvailableSubmitItems.Length);
        int requiredItemCount = Random.Range(1, maxItemsToRequire + 1); 
        
        // 2. 가중치 목록 생성 (점수의 역수를 가중치로 사용)
        List<(Item item, float weight)> weightedPool = new List<(Item, float)>();
        float totalWeight = 0f;

        foreach (Item item in AvailableSubmitItems)
        {
            if (item.itemDataAsset == null) continue; 
            int score = item.itemDataAsset.getScore; 
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
        
        // 3. 가중치 기반으로 랜덤 선택
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
        
        // 4. 목표 점수(100)를 선택된 아이템들에게 분배하여 '요구 수량' 계산
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
            int itemUnitScore = item.itemDataAsset.getScore;
            int requiredScorePortion = scoreAllocations[i];
            
            int requiredCount = Mathf.CeilToInt((float)requiredScorePortion / itemUnitScore);
            
            CurrentRequiredItemsData.Add(item, requiredCount);
            CurrentSubmittedData.Add(item, 0); 

            actualTotalRequiredScore += requiredCount * itemUnitScore;
        }

        Debug.Log($"[GameManager] {GetDayString(CurrentDay)} 납입 요구 목록 생성. 목표 점수: {currentTargetScore}, 실제 요구 점수: {actualTotalRequiredScore}");
    }

    // MARK: 다음 날로 전환
    void NextDay()
    {
        GameDays previousDay = CurrentDay; 
        
        PreviousDayTargetScore = TargetRequiredScore;
        
        PreviousDaySubmittedScore = 0;
        foreach (var pair in CurrentSubmittedData)
        {
            Item item = pair.Key;
            int submittedCount = pair.Value;
            
            // Item 클래스 및 ItemDataAsset 클래스는 외부에서 정의되어 있다고 가정합니다.
            // if (item != null && item.itemDataAsset != null)
            // {
            //     PreviousDaySubmittedScore += submittedCount * item.itemDataAsset.getScore;
            // }
        }
        
        int survivorLoss = CalculateSurvivorLoss();
        
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

        if (CurrentDay != previousDay && CurrentDay <= GameDays.FourthDay)
        {
            // 일차 변경 시 모든 아이템/NPC 영구 데이터 및 현재 씬 오브젝트 초기화
            if (spawnManager != null)
            {
                spawnManager.ResetPersistentData();
                Debug.Log("[GameManager] 일차 변경으로 아이템/NPC/좀비 영구 데이터 및 씬 오브젝트 초기화 완료.");
            }
            
            GenerateRequiredItems(); 
            ShowDayChangeMessage(CurrentDay, survivorLoss); 
        }
    }

    // MARK: 일차 변경 안내문 표시
    public void ShowDayChangeMessage(GameDays newDay, int lossCount)
    {
        // 씬 로드 직후 DayChangePanel이 null일 수 있으므로 다시 확인
        if (DayChangePanel == null || DayChangeText == null)
        {
            Debug.LogWarning("[GameManager] DayChange UI 참조 누락! OnSceneLoaded 복구 로직을 확인하세요.");
            return;
        }
        
        string message;

        if (lossCount > 0)
        {
            message = $"{GetDayString(newDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐. 어제 납입품을 다 못 채워서 {lossCount}명이 사망했어.";
        }
        else
        {
            message = $"{GetDayString(newDay)} 납입품 리스트는 쉘터로 복귀해서 확인해봐. 어제는 수고 많았어. 사망한 생존자는 없어!";
        }
        
        DayChangeText.text = message;
        
        if (dayChangeCoroutine != null)
        {
            StopCoroutine(dayChangeCoroutine); 
        }
        
        dayChangeCoroutine = StartCoroutine(ShowDayChangeCoroutine(lossCount));
    }

    // MARK: 일차 변경 안내 애니메이션 
    private IEnumerator ShowDayChangeCoroutine(int lossCount)
    {
        // Null 체크 유지
        if (dayChangeRectTransform == null || DayChangePanel == null || dayChangeCanvasGroup == null)
        {
            Debug.LogWarning("[ShowDayChangeCoroutine] UI 컴포넌트 참조가 유효하지 않습니다. 코루틴을 중단합니다.");
            dayChangeCoroutine = null;
            yield break;
        }
    
        // ⭐ 수정: 중앙 도착 위치를 initialMidPosition으로 고정
        Vector2 midPos = initialMidPosition; 
        
        // 2. 애니메이션의 '시작 위치' (오른쪽 800.0f)
        Vector2 startPos = new Vector2(midPos.x + 800.0f, midPos.y); 
        // 3. 퇴장 위치 (왼쪽 -2000.0f)
        Vector2 endPos = new Vector2(midPos.x - 2000f, midPos.y); 
        
        dayChangeCanvasGroup.alpha = 1f;
        DayChangePanel.SetActive(true);

        // 애니메이션 시작 전에 RectTransform을 시작 위치로 강제 설정
        if (dayChangeRectTransform != null)
        {
            dayChangeRectTransform.anchoredPosition = startPos;
        }

        // 1. 화면 중앙으로 진입 (Slide In)
        float elapsedTime = 0f;
        
        while (elapsedTime < SlideDuration)
        {
            if (dayChangeRectTransform == null) yield break;

            float t = elapsedTime / SlideDuration;
            dayChangeRectTransform.anchoredPosition = Vector2.Lerp(startPos, midPos, t);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        
        // 최종 위치 보정
        if (dayChangeRectTransform == null) yield break;
        dayChangeRectTransform.anchoredPosition = midPos; 

        // 2. 대기 시간
        yield return new WaitForSeconds(DayChangeDisplayTime);
        
        // 3. 화면 밖 왼쪽으로 퇴장 (Slide Out and Fade Out)
        float fadeOutDuration = SlideDuration;
        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            if (dayChangeRectTransform == null || dayChangeCanvasGroup == null) yield break;
            
            float t = elapsedTime / fadeOutDuration;
            
            // 위치 이동
            dayChangeRectTransform.anchoredPosition = Vector2.Lerp(midPos, endPos, t);
            
            // 페이드 아웃
            dayChangeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        // 4. 최종 정리
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
            case GameDays.FourthDay: return "4일차";
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
            if (spawnManager == null)
            {
                Debug.LogWarning("[GameManager] Main 씬이 로드되었으나 SpawnManager를 찾을 수 없습니다.");
            }
            
            // ⭐ 수정 2: DayChangePanel이 null이 되었을 경우 씬에서 다시 찾아서 할당합니다.
            if (DayChangePanel == null)
            {
                // DayChangePanel은 인스펙터에 할당되지만, 씬 전환 시 파괴되므로 이름으로 다시 찾습니다.
                // 주의: Hierarchy에서 DayChangePanel의 정확한 이름을 사용해야 합니다. 
                GameObject newPanelObject = GameObject.Find("DayChangePanel"); 
                
                if (newPanelObject != null)
                {
                    DayChangePanel = newPanelObject;
                    DayChangeText = DayChangePanel.GetComponentInChildren<Text>(); 
                    dayChangeRectTransform = DayChangePanel.GetComponent<RectTransform>(); 
                    dayChangeCanvasGroup = DayChangePanel.GetComponent<CanvasGroup>();
                    
                    // CanvasGroup이 없으면 추가
                    if (dayChangeCanvasGroup == null)
                    {
                         dayChangeCanvasGroup = DayChangePanel.AddComponent<CanvasGroup>();
                    }
                    
                    // ⭐ 추가: RectTransform 위치를 저장된 초기 위치로 강제 복원
                    if (dayChangeRectTransform != null)
                    {
                        dayChangeRectTransform.anchoredPosition = initialMidPosition;
                        Debug.Log($"[GameManager] UI 위치 강제 복원 완료: {initialMidPosition}");
                    }
                    
                    Debug.Log("[GameManager] Main 씬 로드 후 UI 참조 복구 완료.");
                }
                else
                {
                    Debug.LogWarning("[GameManager] Main 씬 로드 후 DayChangePanel을 찾을 수 없습니다. Hierarchy 이름이 정확한지 확인하세요!");
                }
            }

            // 씬 로드 시 DayChangePanel이 비활성화 상태로 시작하도록 설정
            if (DayChangePanel != null)
            {
                DayChangePanel.SetActive(false);
            }
        } 
        else 
        {
            spawnManager = null;
        }

        ApplyGlobalLight(); 
    }

    public void ResetGameSession()
    {
        Debug.Log("=========================================");
        Debug.Log("[GameManager] 전체 게임 세션 초기화 시작.");
        
        // 1. 상태 변수 초기화
        CurrentEnding = GameEnding.None;
        CurrentDay = GameDays.FirstDay;
        CurrentPhase = Phase.Day;
        IsInShelter = false;
        IsDialogueActive = false;
        
        // 2. 스폰/생존자/점수 초기화
        CurrentZombieSpawnCount = BaseZombieSpawnCount;
        SurvivorCount = InitialSurvivorCount;
        ShelterItemScore = 0;
        PreviousDayTargetScore = 0; 
        PreviousDaySubmittedScore = 0; 

        // 3. 납입 데이터 초기화
        CurrentRequiredItemsData.Clear();
        CurrentSubmittedData.Clear();
        
        // 4. SpawnManager 영구 데이터 초기화
        if (spawnManager != null)
        {
            spawnManager.ResetPersistentData();
        }
        
        Debug.Log("[GameManager] 전체 게임 세션 초기화 완료.");
        Debug.Log("=========================================");
    }

    // 대화 시작 시 호출
    public void StartInteraction()
    {
        IsDialogueActive = true;
        // 다른 모듈 (예: 플레이어)도 이 상태를 확인하여 멈춥니다.
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