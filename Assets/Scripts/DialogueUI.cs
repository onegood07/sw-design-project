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
        // ⭐⭐⭐ 핵심 수정 부분: shouldStartSubmission 플래그를 체크합니다. ⭐⭐⭐
        if (node.shouldStartSubmission && node.questToStart != null)
        {
            Debug.Log("[DialogueUI] 텍스트 출력이 완료되어 퀘스트 납입 UI를 시작합니다.");
            
            // DialogueManager에게 퀘스트 제출 UI를 띄우도록 요청
            // 이 호출에서 DialogueManager는 대화를 닫고 QuestManager를 호출합니다.
            DialogueManager.Instance.StartQuestSubmission(node.questToStart);
            
            return; // 퀘스트 UI가 떴으므로 다른 로직(선택지 등)은 처리하지 않습니다.
        }

        // 퀘스트 연결이 없고, 선택지가 있다면 선택지 버튼을 생성합니다.
        if (node.hasChoices && node.choices != null && node.choices.Length > 0)
        {
            CreateChoices(node.choices);
        }
        // 퀘스트도 없고 선택지도 없다면, 대화를 자동으로 종료합니다.
        else if (node.nextNodeIndex == -1) 
        {
            DialogueManager.Instance.EndDialogue();
        }
        // 그 외의 경우 (다음 노드 인덱스가 있다면), 플레이어의 다음 클릭 입력을 기다립니다. 
        // (이 로직은 보통 DialogueManager의 클릭 처리 함수에서 GoToNextNode를 호출하는 방식으로 처리됩니다.)
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