using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    // 싱글톤
    public static DialogueUI Instance;

    [Header("UI Elements")]
    public GameObject rootPanel; // 대화 UI 전체 패널
    public Text dialogueText; // 대화 내용을 보여줄 Text UI
    public Transform choicesParent; // 선택지 버튼들을 담을 부모 오브젝트
    public GameObject choiceButtonPrefab; // 선택지 버튼 프리팹

    private Coroutine autoAdvanceCoroutine; 
    private Coroutine typingCoroutine; // 타이핑 코루틴 참조

    void Awake()
    {
        Instance = this;  // 싱글톤 초기화
        Hide();           // 시작 시 UI 숨김
    }

    // 대화 UI 보이기
    public void Show()
    {
        rootPanel.SetActive(true);
    }

    // 대화 UI 숨기기
    public void Hide()
    {
        rootPanel.SetActive(false);
    }

    // DialogueNode를 UI에 표시
    public void DisplayNode(DialogueNode node)
    {
        // 이전 선택지 버튼 제거
        foreach (Transform child in choicesParent)
            Destroy(child.gameObject);

        // 기존 타이핑 코루틴이 있으면 중단
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // 새로운 대화 텍스트 타이핑 시작
        typingCoroutine = StartCoroutine(TypeText(node.text, node.typingSpeed));

        // 선택지가 존재할 경우 버튼 생성
        if (node.hasChoices && node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                GameObject btnObj = Instantiate(choiceButtonPrefab, choicesParent); // 버튼 생성
                Text btnText = btnObj.GetComponentInChildren<Text>(); 
                btnText.text = choice.choiceText; // 버튼 텍스트 설정

                Button btn = btnObj.GetComponent<Button>();
                // 버튼 클릭 시 해당 노드로 이동
                btn.onClick.AddListener(() =>
                {
                    DialogueManager.Instance.GoToNextNode(choice.nextNodeIndex);
                });
            }
        }
        else
        {
            // 선택지가 없으면 nextNodeIndex를 따라 다음 노드로 이동
            this.GetComponent<Button>().onClick.RemoveAllListeners();
            this.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager.Instance.GoToNextNode(node.nextNodeIndex);
            });
        }
    }

    // 글자 하나씩 타이핑 효과
    IEnumerator TypeText(string text, float speed)
    {
        dialogueText.text = ""; // 기존 텍스트 초기화
        foreach (char c in text)
        {
            dialogueText.text += c;          // 글자 추가
            yield return new WaitForSeconds(speed); // 지정 속도만큼 대기
        }
    }
}
