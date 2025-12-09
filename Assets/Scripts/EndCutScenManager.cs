using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
public class EndingCutScenManager : MonoBehaviour
{
     [Header("References")]
    public GameObject player;
    public Camera mainCamera;

    [Header("Cutscene Camera Points")]
    public Transform happyCamPoint;
    public Transform badCamPoint;

    [Header("UI")]
    public Canvas endingCanvas;
    public TextMeshProUGUI dialogueText;
    public Image fadeImage;

    bool endingPlayed = false;

    /* ==========================
       외부에서 호출하는 함수
       ========================== */

    public void PlayHappyEnding()
    {
        if (endingPlayed) return;
        endingPlayed = true;
        StartCoroutine(HappyEndingRoutine());
    }

    public void PlayBadEnding()
    {
        if (endingPlayed) return;
        endingPlayed = true;
        StartCoroutine(BadEndingRoutine());
    }

    /* ==========================
       공통 처리
       ========================== */

    void EnterCutsceneMode(Transform camPoint)
    {
        // 조작 차단
        var input = player.GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        // HUD 숨김
        endingCanvas.gameObject.SetActive(true);

        // 카메라 고정
        mainCamera.transform.SetPositionAndRotation(
            camPoint.position,
            camPoint.rotation
        );
    }

    IEnumerator FadeOut(float duration = 2f)
    {
        float t = 0f;
        Color c = fadeImage.color;
        c.a = 0;
        fadeImage.color = c;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fadeImage.color = c;
            yield return null;
        }
    }

    /* ==========================
       해피 엔딩
       ========================== */

    IEnumerator HappyEndingRoutine()
    {
        EnterCutsceneMode(happyCamPoint);

        dialogueText.text = "약속된 날, 드디어 무전기에서 무언가 들리기 시작했다.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "- 아아, 들리십니까? 정부군입니다. 생존 신호 확인 바랍니다.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "곧 군용 차량이 도착합니다. 빠른 이동과 피해 최소화를 위해 준비해주십시오.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "우리는 드디어 이 지옥 같은 도시에서 벗어날 수 있었다.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "우리가 향하는 곳이 어딘지는 알 수 없었지만......";
        yield return new WaitForSeconds(3f);

        yield return FadeOut();

        // 여기서 크레딧 씬 전환 or 게임 종료
        Debug.Log("HAPPY END");
    }

    /* ==========================
       배드 엔딩
       ========================== */

    IEnumerator BadEndingRoutine()
    {
        EnterCutsceneMode(badCamPoint);

        dialogueText.text = "…여기 아직 살아 있는 사람이 있다.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "(응답 없음)";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "도시는 그를 놓아주지 않았다.";
        yield return new WaitForSeconds(3f);

        yield return FadeOut();

        Debug.Log("BAD END");
    }
}
