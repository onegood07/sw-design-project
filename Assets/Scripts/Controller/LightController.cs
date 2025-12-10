using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;


public class LightController : MonoBehaviour
{
    public static LightController Instance;

    [Header("전역 라이트 및 기본 시간 설정")]
    public Light2D globalLight;

    public float transitionDuration = 4f; // 일반 전환 시간 (일출 초기, 저녁->밤 전환 등에 사용)

    // MARK: 낮 페이즈 전환 (Night -> Day) 세부 설정
    [Header("🌅 Day Phase (일출) 전환 설정")]
    public float dawnHoldDuration = 10f; // 붉은 새벽 조명 유지 시간 (10초)
    public float dawnToMidDayTransitionDuration = 5f; // 새벽 -> 한낮으로 전환하는 시간
    
    [Tooltip("붉은 빛 새벽 조명")]
    public float dawnIntensity = 1.0f;
    public Color dawnColor = new Color(1.0f, 0.5f, 0.3f); 
    
    [Tooltip("완전 밝은 한낮 조명")]
    public float midDayIntensity = 1.0f; // Day Phase의 최종 목표 밝기
    public Color midDayColor = Color.white; // Day Phase의 최종 목표 색상

    // MARK: 밤 페이즈 전환 (Day -> Night) 세부 설정
    [Header("🌆 Night Phase (일몰) 전환 설정")]
    public float midDayToDuskTransitionDuration = 10f; // 한낮 -> 저녁 노을로 서서히 어두워지는 시간 (10초)
    
    [Tooltip("저녁 (Dusk) 조명")]
    public float duskIntensity = 0.7f; // 밤보다 밝고 낮보다 어두운 저녁 밝기
    public Color duskColor = new Color(1.0f, 0.6f, 0.3f); // 노란빛/오렌지빛 저녁 색상

    [Tooltip("매우 어두운 밤 조명")]
    public float nightIntensity = 0.05f; 
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f); 

    // 암전 단계 관련 (사용되지 않지만 변수는 유지)
    [Header("암전(Blackout) 설정")]
    public float blackoutDuration = 1f; 
    public Color blackoutColor = Color.black; 
    public float blackoutIntensity = 0f; 

    private Coroutine lightCoroutine; 

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (globalLight == null)
        {
            globalLight = GetComponent<Light2D>();
        }
    }
    
    // GameManager에서 Phase.Day 또는 Phase.Night로 호출
    public void UpdateGlobalLight(Phase newPhase)
    {
        if (globalLight == null) return;
        
        if (lightCoroutine != null)
        {
            StopCoroutine(lightCoroutine);
        }

        if (newPhase == Phase.Night)
        {
            // Day -> Night 전환: 10초 저녁 노을 -> 4초 밤 전환
            lightCoroutine = StartCoroutine(TransitionDayToNightComplexSequence());
        }
        else // newPhase == Phase.Day
        {
            // Night -> Day 전환: 4초 새벽으로 전환 -> 10초 새벽 유지 -> 한낮 전환
            lightCoroutine = StartCoroutine(TransitionNightToDayComplexSequence());
        }
    }

    // 일반적인 선형 전환 코루틴 (Lerp)
    private IEnumerator TransitionLight(float targetIntensity, Color targetColor, float duration)
    {
        float timeElapsed = 0f;
        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;
        
        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            globalLight.color = Color.Lerp(startColor, targetColor, t);
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 보정
        globalLight.intensity = targetIntensity;
        globalLight.color = targetColor;
    }

    // MARK: 밤 -> 낮 (일출) 복합 전환 시퀀스
    private IEnumerator TransitionNightToDayComplexSequence()
    {
        // 1. **(수정 적용)** 4초 동안 서서히 밝아짐 (현재 밤색 -> 붉은 새벽 Dawn 색상)
        // Night 상태에서 Dawn 상태로 바로 부드럽게 전환합니다.
        // transitionDuration을 4f로 설정하여 사용합니다.
        yield return TransitionLight(dawnIntensity, dawnColor, transitionDuration);
        
        // 2. 10초 동안 붉은 새벽 상태 유지
        yield return new WaitForSeconds(dawnHoldDuration);
        
        // 3. 5초 동안 서서히 완전한 낮 (MidDay)으로 전환
        yield return TransitionLight(midDayIntensity, midDayColor, dawnToMidDayTransitionDuration);
        
        lightCoroutine = null;
    }
    
    // MARK: 낮 -> 밤 (일몰) 복합 전환 시퀀스
    private IEnumerator TransitionDayToNightComplexSequence()
    {
        // 1. 10초 동안 서서히 어두워져 저녁 (Dusk) 색상으로 전환
        yield return TransitionLight(duskIntensity, duskColor, midDayToDuskTransitionDuration);

        // 2. 4초 동안 서서히 짙은 밤 (Night)으로 전환
        // transitionDuration을 4f로 설정하여 사용합니다.
        yield return TransitionLight(nightIntensity, nightColor, transitionDuration);
        
        lightCoroutine = null;
    }
}