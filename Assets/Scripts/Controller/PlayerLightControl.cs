using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement; 

public class PlayerLightControl : MonoBehaviour
{
    // 싱글톤 선언
    public static PlayerLightControl Instance { get; private set; }
    public bool isLanternActive {get;private set;} = false; // 랜턴 사용 여부
    public Light2D playerLight; 

    [Header("반경 설정")]
    public float baseRadius = 4f;        // 랜턴 미사용 시 기본 반경
    public float lanternRadius = 7f;     // 랜턴 사용 시 확장된 반경
    public float transitionSpeed = 100f;   // 반경 전환 속도

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // 씬이 변경되어도 유지하고 싶다면 DontDestroyOnLoad(gameObject); 추가
        }
    }
    void Start()
    {
        // 처음에는 조명을 꺼둔 상태로 시작
        if (playerLight != null)
        {
            playerLight.gameObject.SetActive(false);
            playerLight.pointLightOuterRadius = baseRadius; // 기본 반경으로 세팅
        }
    }

    void Update()
    {
        // GameManager나 조명이 없으면 라이트 끄고 종료
        if (GameManager.Instance == null || playerLight == null)
        {
            if (playerLight != null && playerLight.gameObject.activeSelf)
            {
                playerLight.gameObject.SetActive(false);
            }
            return;
        }

        // 현재 씬이 쉘터인지 확인
        string currentScene = SceneManager.GetActiveScene().name;
        bool isInShelter = (currentScene == GameManager.Instance.ShelterSceneName);

        // 현재 페이즈가 밤인지 확인
        bool isNight = (GameManager.Instance.CurrentPhase == Phase.Night);
        
        // 밤이고, 쉘터 내부가 아닐 때만 플레이어 라이트 켜기
        bool shouldBeActive = isNight && !isInShelter;

        
        // 활성 상태가 다르면 켜거나 끔
        if (playerLight.gameObject.activeSelf != shouldBeActive)
        {
            playerLight.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive) return; // 조명이 꺼진 상태라면 나머지 로직 중단
        }

        // 라이트가 켜져있을 때만 반경 조정
        if (shouldBeActive)
        {
            // 랜턴 상태에 따라 목표 반경 설정
            float targetRadius = isLanternActive ? lanternRadius : baseRadius;
            
            // 플레이어 반경과 설정한 목표 반경이 다르면 부드럽게 반경 전환
            if (playerLight.pointLightOuterRadius != targetRadius)
            {
                // moveTowards 로 변경
                playerLight.pointLightOuterRadius = Mathf.MoveTowards(
                playerLight.pointLightOuterRadius, 
                targetRadius, 
                Time.deltaTime * transitionSpeed * 4f // transitionSpeed를 직접 속도로 사용 (필요 시 속도 조정)
                );
            }
        }
    }

    // TODO: 랜턴 아이템 연결 시 사용할 함수
    // 외부에서 호출하여 랜턴 효과 적용
    public void SetLanternActive()
    {
        isLanternActive = !isLanternActive;
        Debug.Log("setLanternActive 실행");
    }

}