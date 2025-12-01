using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Coroutine을 사용하려면 이 네임스페이스가 필수입니다.

/// <summary>
/// 대화 창을 표시하고 선택지 버튼을 관리하는 싱글톤입니다.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI Elements")]
    public GameObject rootPanel; // 대화 UI 전체를 감싸는 패널
    public Text dialogueText;    // 대화 텍스트 표시 영역
    public Transform choicesParent; // 선택지 버튼들이 배치될 부모 오브젝트
    public GameObject choiceButtonPrefab; // 선택지 버튼 프리팹

    private DialogueNode currentNode;
    private Coroutine typeTextCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Hide(); // 시작 시 숨기기
    }

    public void Show()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        ClearChoices();
    }

    /// <summary>
    /// 특정 DialogueNode의 내용을 화면에 표시합니다.
    /// </summary>
    public void DisplayNode(DialogueNode node)
    {
        currentNode = node;

        // 기존 타이핑 코루틴이 실행 중이면 중지
        if (typeTextCoroutine != null)
        {
            StopCoroutine(typeTextCoroutine);
        }
        
        ClearChoices();

        // 텍스트 타이핑 효과 시작
        typeTextCoroutine = StartCoroutine(TypeText(node));
    }

    /// <summary>
    /// 텍스트를 한 글자씩 타이핑하는 효과를 처리합니다.
    /// </summary>
    IEnumerator TypeText(DialogueNode node)
    {
        dialogueText.text = "";
        // typingSpeed가 0보다 크면 그 속도를 사용하고, 아니면 기본 0.05초 사용
        float delay = node.typingSpeed > 0 ? node.typingSpeed : 0.05f; 
        
        foreach (char c in node.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }
        
        // 텍스트 출력이 완료된 직후 후속 로직을 처리합니다.
        HandlePostTextDisplay(node);
    }
    
    /// <summary>
    /// 텍스트 출력이 완료된 후 퀘스트, 선택지, 다음 노드 이동 등을 처리합니다.
    /// </summary>
    private void HandlePostTextDisplay(DialogueNode node)
    {
        // 1. 퀘스트 연결 확인 (questToStart는 DialogueNode에 정의된 필드)
        if (node.questToStart != null)
        {
            // DialogueManager에게 퀘스트 제출 UI를 띄우도록 요청
            DialogueManager.Instance.StartQuestSubmission(node.questToStart);
            return; 
        }

        // 2. 퀘스트 연결이 없고, 선택지가 있다면 선택지 버튼을 생성합니다. (hasChoices는 DialogueNode에 정의된 필드)
        if (node.hasChoices && node.choices != null && node.choices.Length > 0)
        {
            CreateChoices(node.choices);
        }
        // 3. 퀘스트도 없고 선택지도 없다면, 다음 노드 인덱스를 확인합니다.
        // nextNodeIndex가 -1이면 대화 종료 (nextNodeIndex는 DialogueNode에 정의된 필드)
        else if (node.nextNodeIndex == -1) 
        {
            DialogueManager.Instance.EndDialogue();
        }
        // 4. 다음 노드 인덱스가 있다면 (즉, nextNodeIndex > -1), 플레이어의 클릭 입력 대기 (DialogueManager에서 처리)
    }

    /// <summary>
    /// 선택지 버튼들을 생성하고 리스너를 연결합니다.
    /// </summary>
    private void CreateChoices(DialogueChoice[] choices)
    {
        for (int i = 0; i < choices.Length; i++)
        {
            DialogueChoice choice = choices[i];
            
            if (choiceButtonPrefab == null)
            {
                Debug.LogError("[DialogueUI] Choice Button Prefab이 연결되지 않았습니다.");
                return;
            }

            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesParent);
            
            // 버튼 텍스트 설정
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                // choiceText는 DialogueChoice에 정의된 필드
                buttonText.text = choice.choiceText; 
            }
            
            Button btn = buttonObj.GetComponent<Button>();
            
            // 버튼 클릭 시 다음 노드로 이동 요청
            // nextNodeIndex는 DialogueChoice에 정의된 필드
            btn.onClick.AddListener(() => OnChoiceSelected(choice.nextNodeIndex)); 
        }
    }

    /// <summary>
    /// 현재 표시된 모든 선택지 버튼을 제거합니다.
    /// </summary>
    private void ClearChoices()
    {
        for (int i = choicesParent.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesParent.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 선택지 버튼 클릭 시 호출되어 DialogueManager에게 다음 노드 인덱스를 전달합니다.
    /// </summary>
    public void OnChoiceSelected(int nextNodeIndex)
    {
        ClearChoices(); // 선택지를 지우고
        DialogueManager.Instance.GoToNextNode(nextNodeIndex); // 다음 노드로 이동
    }
}