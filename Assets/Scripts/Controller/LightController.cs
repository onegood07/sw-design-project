using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightController : MonoBehaviour
{
    public static LightController Instance;

    [Header("전체 글로벌 라이트 설정")]

    public Light2D globalLight; // 전체 환경을 비추는 라이트
    public float transitionDuration = 2f; // 낮과 밤 전환에 걸리는 시간


    [Header("낮과 밤 밝기 및 색상")]
    [Tooltip("낮과 밤 밝기(Intensity) 및 색상(Color) 설정")]
    public float dayIntensity = 1.0f; // 낮일 때 밝기
    public Color dayColor = Color.white; // 낮일 때 색상

    public float nightIntensity = 0.2f; // 밤일 때 밝기
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f); // 밤일 때 색상

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

        // Phase에 따라 목표 밝기와 색상 설정
        float targetIntensity = (newPhase == Phase.Night) ? nightIntensity : dayIntensity;
        Color targetColor = (newPhase == Phase.Night) ? nightColor : dayColor;

        // 라이트 전환 시작
        lightCoroutine = StartCoroutine(TransitionLight(targetIntensity, targetColor));
    }

    // 라이트 전환 코루틴
    private IEnumerator TransitionLight(float targetIntensity, Color targetColor)
    {
        float timeElapsed = 0f; // 시간이 얼마나 흘렀는지 담는 변수
        float startIntensity = globalLight.intensity; // 현재 밝기
        Color startColor = globalLight.color; // 현재 색상
        
        // 밝기의 색상이 거의 동일하면 전환 생략 후 즉시 적용
        if (Mathf.Abs(startIntensity - targetIntensity) < 0.01f)
        {
            globalLight.intensity = targetIntensity;
            globalLight.color = targetColor;
            yield break;
        }

        // 지정된 시간 동안 서서히 밝기/색상 변화
        while (timeElapsed < transitionDuration)
        {
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, timeElapsed / transitionDuration);
            globalLight.color = Color.Lerp(startColor, targetColor, timeElapsed / transitionDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 보정 적용
        globalLight.intensity = targetIntensity;
        globalLight.color = targetColor;
        lightCoroutine = null; // 코루틴 종료 처리
    }
}