using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndCutsceneController : MonoBehaviour
{
    [Header("Main References")]
    public Camera mainCamera;
    public GameObject player;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public Text dialogueText;

    [Header("Cutscene Objects")]
    public GameObject militaryTruckPrefab;
    public Vector3 truckSpawnPosition;
    public Vector3 truckSpawnRotation;


    GameObject spawnedTruck;

    // ===========================
    // PUBLIC ENTRY
    // ===========================

    public void PlayHappyEnding()
    {
        StartCoroutine(HappyEnding());
    }

    public void PlayBadEnding()
    {
        StartCoroutine(BadEnding());
    }

    // ===========================
    // HAPPY ENDING
    // ===========================

    IEnumerator HappyEnding()
    {
        DisablePlayerControl();

        spawnedTruck = Instantiate(
            militaryTruckPrefab,
            truckSpawnPosition,
            Quaternion.Euler(truckSpawnRotation)
        );



        dialoguePanel.SetActive(true);
        dialogueText.text = "드디어 무전기에서 소리가 들리기 시작했다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "-아아, 여기는 정부군부대. 생존자는 응답 바랍니다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "곧 군용 차량이 쉘터 앞으로 도착할 예정이니 준비해 주십시오.";
        yield return new WaitForSecondsRealtime(3f);

        // 트럭 이동 연출
        yield return StartCoroutine(MoveTruck(spawnedTruck.transform));

        dialogueText.text = "마침내 우리는 이 도시에서 탈출할 수 있었다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "비록 우리가 향하는 곳이 어디인지, 아무도 알지 못했지만......";
        yield return new WaitForSecondsRealtime(3f);

        EndCutscene();
    }

    // ===========================
    // BAD ENDING
    // ===========================

    IEnumerator BadEnding()
    {
        DisablePlayerControl();

        dialoguePanel.SetActive(true);
        dialogueText.text = "... 여기 아직 생존자가 있습니다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "계속 연락해 봤지만 응답은 없었다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "결국 나는 이 지옥같은 도시에 혼자 남겨졌다.";
        yield return new WaitForSecondsRealtime(3f);

        dialogueText.text = "혼자서는 절대 살아남을 수 없다.";
        yield return new WaitForSecondsRealtime(3f);

        EndCutscene();
    }

    // ===========================
    // COMMON
    // ===========================

    void DisablePlayerControl()
    {
        if (player != null)
            player.SetActive(false);

        Time.timeScale = 0f;
    }

    void EndCutscene()
    {
        Debug.Log("[EndCutscene] 컷씬 종료");

        // 여기서 페이드아웃 / 엔딩 크레딧 / 메인 메뉴 이동 등 처리 가능

        // 페이드 아웃 후 메인 메뉴로
        Time.timeScale = 1f;
        FadeManager.Instance.FadeOutToScene("StartMenu");
    }

    // ===========================
    // OPTIONAL: TRUCK MOVE
    // ===========================

    IEnumerator MoveTruck(Transform truck)
    {
        Vector3 start = truck.position;
        Vector3 end = start + truck.forward * 6f;
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            truck.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        truck.position = end;
    }

}
