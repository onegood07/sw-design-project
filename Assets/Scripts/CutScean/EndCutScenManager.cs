using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class EndCutsceneController : MonoBehaviour
{
    [Header("Main References")]
    public Camera mainCamera;
    public GameObject player;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public Text dialogueText;

    [Header("Cutscene Objects")]
    public GameObject vehicleGroupPrefab;
    public Vector3 truckSpawnPosition;
    public Vector3 truckSpawnRotation;

    [Header("Dialogue")]
    public TypingEffect typingEffect;



    GameObject spawnedTruck;

    // ===========================
    // PUBLIC ENTRY
    // ===========================

    void FreezeWorld()
    {
        Time.timeScale = 0f;
    }

    void UnfreezeWorld()
    {
        Time.timeScale = 1f;
    }

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
        Debug.LogError("🔥 HappyEnding CALLED\n" + System.Environment.StackTrace);
        DisablePlayerControl();

        FreezeWorld();
        dialoguePanel.SetActive(true);

        spawnedTruck = Instantiate(
            vehicleGroupPrefab,
            truckSpawnPosition,
            Quaternion.Euler(truckSpawnRotation)
        );

        if (spawnedTruck == null)
        {
            Debug.LogError("[EndCutscene] vehicleGroupPrefab instantiate failed");
            yield break;
        }

        Debug.Log($"[Cutscene] spawnedTruck = {spawnedTruck}");
        

        if (spawnedTruck != null)
            Debug.Log($"[Cutscene] child count = {spawnedTruck.transform.childCount}");


        yield return ShowDialogue("드디어 무전기에서 소리가 들리기 시작했다.");

        yield return ShowDialogue("-아아, 여기는 정부군부대. 생존자는 응답 바랍니다.");

        yield return ShowDialogue("곧 군용 차량이 쉘터 앞으로 도착할 예정이니 준비해 주십시오.");


        // 트럭 이동 연출
        Transform mover = spawnedTruck.transform;

        if (mover == null)
        {
            Debug.LogError("[EndCutscene] mover is null");
            yield break;
        }

        yield return StartCoroutine(MoveTruck(mover));

        yield return ShowDialogue("마침내 우리는 이 도시에서 탈출할 수 있었다.");
    
        yield return ShowDialogue("비록 우리가 향하는 곳이 어디인지, 아무도 알지 못했지만......");
    
        EndCutscene();
    }

    // ===========================
    // BAD ENDING
    // ===========================
    IEnumerator BadEnding()
    {
        DisablePlayerControl();

        FreezeWorld();
        dialoguePanel.SetActive(true);

        yield return ShowDialogue("... 여기 아직 생존자가 있습니다.");
   
        yield return ShowDialogue("- ...");
   
        yield return ShowDialogue("계속 연락해 봤지만 응답은 없었다.");

        yield return ShowDialogue("결국 나는 이 지옥같은 도시에 혼자 남겨졌다.");

        yield return ShowDialogue("혼자서는 절대 살아남을 수 없다.");

        EndCutscene();
    }

    // ===========================
    // COMMON
    // ===========================

    void DisablePlayerControl()
    {
        var controller = player.GetComponent<HeroMoveControl>();
        if (controller != null)
            controller.enabled = false;
    }

    void EndCutscene()
    {
        UnfreezeWorld();
        Debug.Log("[EndCutscene] 컷씬 종료");

        if (spawnedTruck != null)
            Destroy(spawnedTruck);
        
        dialoguePanel.SetActive(false);

        // 여기서 페이드아웃 / 엔딩 크레딧 / 메인 메뉴 이동 등 처리 가능
        EnablePlayerControl(); // ← 추가
        // 페이드 아웃 후 메인 메뉴로
        FadeManager.Instance.FadeOutToScene("StartMenu");
    }

    IEnumerator ShowDialogue(string message)
    {
        bool isDone = false;

        if (typingEffect == null)
        {
            Debug.LogError("[EndCutscene] TypingEffect missing");
            dialogueText.text = message;
            yield return null;
            yield break;
        }

        void OnFinished()
        {
            isDone = true;
        }

        typingEffect.onTypingFinished += OnFinished;
        typingEffect.Play(message);

        // 버튼 클릭 or 키 입력 대기
        yield return new WaitUntil(() => isDone);
        typingEffect.onTypingFinished -= OnFinished;
    }

    void EnablePlayerControl()
    {
        var controller = player.GetComponent<HeroMoveControl>();
        if (controller != null)
            controller.enabled = true;
    }


    // ===========================
    // OPTIONAL: TRUCK MOVE
    // ===========================

    IEnumerator MoveTruck(Transform truck)
    {
        Vector3 start = truck.position;
        Vector3 end = start + truck.forward * 70f;

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
    
   // debuging
    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
            PlayHappyEnding();

        if (Keyboard.current.bKey.wasPressedThisFrame)
            PlayBadEnding();
    }
}
