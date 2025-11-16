using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        // 낮/밤 페이즈 확인 
        bool isNight = (GameManager.Instance.CurrentPhase == Phase.Night);
        
        // 밤에만 조명을 활성화/비활성화
        if (playerLight.gameObject.activeSelf != isNight)
        {
            playerLight.gameObject.SetActive(isNight);
            if (!isNight) return; // 낮이면 조명 로직 중단
        }

        // 반경 조절 로직 (밤에만 실행))
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