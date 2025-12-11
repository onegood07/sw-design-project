using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static FadeManager Instance;

    
    [Header("Fade Image Setting")]
    public Image fadeImage; // 화면 전체를 덮는 Image (Canvas에 있어야 합니다)
    public float fadeDuration = 1f; // 페이드 전체에 걸리는 시간

    // ⭐ 추가: 씬 전환 후 플레이어가 스폰될 목표 위치
    private Vector3 targetSpawnPosition = Vector3.zero;

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

    // MARK: - 외부 호출 함수

    // 1. 일반 씬 전환 (위치 지정 없음)
    public void FadeOutToScene(string sceneName)
    {
        // 위치 초기화 및 일반 페이드 시작
        targetSpawnPosition = Vector3.zero;
        StartCoroutine(FadeOutIn(sceneName));
    }
    
    // ⭐ 2. 위치 지정 포함 씬 전환 (쉘터 출입 시 사용)
    /// <summary>
    /// 목표 씬으로 전환하면서 플레이어의 정확한 스폰 위치를 지정합니다.
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    /// <param name="spawnPosition">새 씬에서 플레이어가 배치될 월드 좌표</param>
    public void FadeOutToScene(string sceneName, Vector3 spawnPosition)
    {
        // 목표 위치 저장 및 페이드 시작
        targetSpawnPosition = spawnPosition;
        StartCoroutine(FadeOutIn(sceneName));
    }

    // MARK: - 코루틴 로직

    // 페이드 아웃(어둡게) -> 씬 전환 -> 위치 적용 -> 페이드 인 (밝게)
    private IEnumerator FadeOutIn(string sceneName)
    {
        // 1. 화면 어둡게 만들기 (알파값을 0 -> 1로 바꿔서 화면 가림)
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. 씬 전환 
        SceneManager.LoadScene(sceneName);

        // 3. 씬이 완전히 적용될 때까지 프레임 대기 (새 씬의 Awake/Start 완료 대기)
        yield return null;

        // ⭐ 4. 위치 적용: 씬 로드 직후, 페이드 인이 시작되기 전에 플레이어 위치 조정
        if (targetSpawnPosition != Vector3.zero)
        {
            // HeroMoveControl.Instance가 새 씬에서 찾아졌는지 확인해야 합니다.
            if (HeroMoveControl.Instance != null)
            {
                // HeroMoveControl의 ForceMove를 사용하여 위치를 강제 변경합니다.
                HeroMoveControl.Instance.ForceMove(targetSpawnPosition);
                Debug.Log($"[FadeManager] 플레이어 위치를 지정된 좌표 {targetSpawnPosition}로 이동.");
            }
            // 위치 적용 후, 다음 씬 전환에 영향이 없도록 초기화
            targetSpawnPosition = Vector3.zero; 
        }

        // 5. 씬 전환 후, 게임 전체 상태(전역 라이트) 재적용
       GameManager.Instance?.ApplyGlobalLight(true);

        // 6. 화면 밝게
        yield return StartCoroutine(Fade(1f, 0f));
    }

    // 실제 알파 값을 시간에 따라 보간해서 변경하는 코루틴
    private IEnumerator Fade(float start, float end)
    {
        float t = 0f; // 경과 시간 누적
        Color c = fadeImage.color; // 이미지 컬러

        if (fadeImage == null)
        {
            Debug.LogWarning("[FadeManager] fadeImage가 할당되지 않았습니다. 페이드 동작을 수행할 수 없습니다.");
            yield break;
        }
        
        while (t < fadeDuration)
        {
            t += Time.deltaTime; // 프레임 경과 시간 누적
            c.a = Mathf.Lerp(start, end, t / fadeDuration); // 선형 보간으로 알파값 계산

            fadeImage.color = c; // 변경된 알파값 이미지 적용
            yield return null; // 다음 프레임까지 대기
        }

        // 루프를 빠져나온 후 오차 보정하여 정확한 최종 알파값으로 보정
        c.a = end;
        fadeImage.color = c;
    }
}