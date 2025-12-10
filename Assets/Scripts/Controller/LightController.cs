using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightController : MonoBehaviour
{
    public static LightController Instance;

    [Header("전체 글로벌 라이트 설정")]
    public Light2D globalLight; // 전체 환경을 비추는 라이트 
    public float transitionDuration = 2f; // 낮과 밤 전환에 걸리는 시간 (각 단계에 사용)


    [Header("낮과 밤 밝기 및 색상")]
    [Tooltip("낮과 밤 밝기(Intensity) 및 색상(Color) 설정")]
    public float dayIntensity = 1.0f; // 낮일 때 밝기 (1.0f 유지)
    // **수정: 해 뜨는 느낌의 붉은 계열 색상 (Warm Orange/Red)**
    public Color dayColor = new Color(1.0f, 0.7f, 0.5f); // 낮일 때 색상 (주황빛 새벽) 

    public float nightIntensity = 0.05f; // 밤일 때 밝기 (매우 어두움)
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f); // 밤일 때 색상

    // **새로 추가된 설정:** 암전 단계 관련
    [Header("암전(Blackout) 설정")]
    public float blackoutDuration = 2f; // 암전 유지 시간
    private Color blackoutColor = Color.black; // 암전 시 색상 (검은색)
    private float blackoutIntensity = 0f; // 암전 시 밝기 (0)

    private Coroutine lightCoroutine; // 낮과 밤 전환 코루틴

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 인스펙터에 라이트가 없으면 자동으로 라이트를 가져옴
        if (globalLight == null)
        {
            globalLight = GetComponent<Light2D>();
        }
    }
    
    // GameManager에서 호출하여 라이트 상태를 바꾸는 함수
    public void UpdateGlobalLight(Phase newPhase)
    {
        if (globalLight == null) return;
        
        // 이미 전환 중이면 이전 코루틴 중단
        if (lightCoroutine != null)
        {
            StopCoroutine(lightCoroutine);
        }
        
        // **로직 분기:** 낮 -> 밤 전환 (선형) vs. 밤 -> 낮 전환 (복합 시퀀스)
        if (newPhase == Phase.Night)
        {
            // 낮 -> 밤 전환: 기존의 선형 TransitionLight 사용
            lightCoroutine = StartCoroutine(TransitionLight(nightIntensity, nightColor, transitionDuration));
        }
        else // newPhase == Phase.Day
        {
            // 밤 -> 낮 전환: 새로운 복합 시퀀스 사용 (어두워짐 -> 암전 -> 밝아짐)
            lightCoroutine = StartCoroutine(TransitionNightToDayWithBlackout());
        }
    }

    // 일반적인 라이트 전환 코루틴 (주로 낮 -> 밤 전환에 사용)
    private IEnumerator TransitionLight(float targetIntensity, Color targetColor, float duration)
    {
        float timeElapsed = 0f;
        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;
        
        // 지정된 시간 동안 서서히 밝기/색상 변화
        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            globalLight.color = Color.Lerp(startColor, targetColor, t);
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 보정 적용
        globalLight.intensity = targetIntensity;
        globalLight.color = targetColor;
        // 코루틴 종료 처리는 이 함수를 호출한 상위 코루틴이 담당할 수도 있으므로 여기서는 생략
    }

    // **밤 -> 낮 복합 전환 코루틴 (요청하신 로직)**
    private IEnumerator TransitionNightToDayWithBlackout()
    {
        // 1. 2초 동안 서서히 어두워짐 (현재 밤색 -> 완전 검은색)
        yield return TransitionLight(blackoutIntensity, blackoutColor, transitionDuration);
        
        // 2. 2초 동안 암전 유지
        yield return new WaitForSeconds(blackoutDuration);

        // 3. 2초 동안 서서히 밝아짐 (완전 검은색 -> 붉은 새벽 낮색)
        yield return TransitionLight(dayIntensity, dayColor, transitionDuration);
        
        lightCoroutine = null; // 모든 시퀀스 종료 처리
    }
}