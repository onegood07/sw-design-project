using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    // 싱글톤 인스턴스 - 다른 스크립트에서 FadeManager.Instance로 접근하여 호출 가능하도록 구성
    public static FadeManager Instance;

    
    [Header("Fade Image Setting")]
    public Image fadeImage; // 화면 전체를 덮는 Image로써, 알파값을 조정해 페이드 효과 구현 (A 값 조정)
    public float fadeDuration = 1f; // 페이드 전체에 걸리는 시간 (어둡게/밝게 전환하는데 소요되는 시간)

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 X
        }
        else {
            Destroy(gameObject); // 중복 인스턴스 제거
        }
    }

    // 외부에서 호출 -> 특정 씬으로 페이드 아웃 후 씬 전환, 이후 페이드 인
    public void FadeOutToScene(string sceneName)
    {
        StartCoroutine(FadeOutIn(sceneName));
    }

    // 페이드 아웃(어둡게) -> 씬 전환 -> 페이드 인 (밝게)
    private IEnumerator FadeOutIn(string sceneName)
    {
        // 화면 어둡게 만들기 (알파값을 0 -> 1로 바꿔서 화면 가림)
        yield return StartCoroutine(Fade(0f, 1f));

        // 씬 전환 
        SceneManager.LoadScene(sceneName);

        // 씬이 완전히 적용될 때까지 프레임 대기 
        // 새로 로드한 씬의 Awake, Start 함수 등이 적용되도록 대기
        yield return null;

        // 씬 전환 후, 게임 전체 상태(전역 라이트) 재적용
        GameManager.Instance?.ApplyGlobalLight();

        // 화면 밝게
        yield return StartCoroutine(Fade(1f, 0f));
    }

    // 실제 알파 값을 시간에 따라 보간해서 변경하는 코루틴
    // start는 초기값, end는 목표 알파값 -> 0은 투명, 1은 불투명
    private IEnumerator Fade(float start, float end)
    {
        float t = 0f; // 경과 시간 누적
        Color c = fadeImage.color; // 이미지 컬러

        // fadeImage가 설정되어 있지 않으면 더 이상 진행 X (오류 방지)
        if (fadeImage == null)
        {
            Debug.LogWarning("[FadeManager] fadeImage가 할당되지 않았습니다. 페이드 동작을 수행할 수 없습니다.");
            yield break;
        }
        
        // t가 fadeDuration이 될 때까지(즉 전체 시간 동안) 매 프레임마다 알파값 보간
        while (t < fadeDuration)
        {
            t += Time.deltaTime; // 프레임 경과 시간 누적
            c.a = Mathf.Lerp(start, end, t / fadeDuration); // 선형 보간으로 알파값 계산

            fadeImage.color = c; // 변경된 알파값 이미지 적용
            yield return null; // 다음 프레임까지 대기
        }

        // 루프를 빠져나온 후 오차 보정하여 정확한 최종 알파값으로 보정
        // Lerp와 Time.deltaTime 계산을 하면 소수점 오차 발생 우려 -> end로 정확히 목표 알파로 맞춰서 안전하게 설정
        c.a = end;
        fadeImage.color = c;
    }
}
