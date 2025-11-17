using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement; // 💡 SceneManager 사용을 위해 추가

public class PlayerLightControl : MonoBehaviour
{
    public Light2D playerLight; 

    [Header("Radius Settings")]
    public float baseRadius = 4f;        // 랜턴 미사용 시 기본 반경
    public float lanternRadius = 8f;     // 랜턴 사용 시 확장된 반경
    public float transitionSpeed = 5f;   // 반경 전환 속도

    private bool isLanternActive = false; // 랜턴 사용 상태

    void Start()
    {
        // Light 오브젝트를 초기에는 비활성화 상태로 시작
        if (playerLight != null)
        {
            playerLight.gameObject.SetActive(false);
            playerLight.pointLightOuterRadius = baseRadius; 
        }
    }

    void Update()
    {
        // GameManager 유효성 검사
        if (GameManager.Instance == null || playerLight == null)
        {
            // GameManager가 없으면 조명을 끄고 리턴
            if (playerLight != null && playerLight.gameObject.activeSelf)
            {
                playerLight.gameObject.SetActive(false);
            }
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool isInShelter = (currentScene == GameManager.Instance.ShelterSceneName);

        // 밤 페이즈 확인
        bool isNight = (GameManager.Instance.CurrentPhase == Phase.Night);
        
        // 밤이면서, 쉘터 내부가 아닐 때
        bool shouldBeActive = isNight && !isInShelter;

        
        // 활성화/비활성화 로직
        if (playerLight.gameObject.activeSelf != shouldBeActive)
        {
            playerLight.gameObject.SetActive(shouldBeActive);
            if (!shouldBeActive) return; // 조명이 꺼졌다면 나머지 로직 중단
        }

        if (shouldBeActive)
        {
            float targetRadius = isLanternActive ? lanternRadius : baseRadius;
            
            // 부드럽게 반경 전환
            if (playerLight.pointLightOuterRadius != targetRadius)
            {
                playerLight.pointLightOuterRadius = Mathf.Lerp(
                    playerLight.pointLightOuterRadius, 
                    targetRadius, 
                    Time.deltaTime * transitionSpeed
                );
            }
        }
    }

    // 외부에서 호출하여 랜턴 효과 적용
    public void SetLanternActive(bool active)
    {
        isLanternActive = active;
    }

    private void ToggleLantern()
    {
        isLanternActive = !isLanternActive;
    }
}