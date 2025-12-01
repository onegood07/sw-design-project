using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData; // 현재 DialogueData
    private int currentNodeIndex; // 현재 노드 인덱스
    private DialogueNPC currentNPC; // 현재 NPC
    
    // 현재 대화가 활성 상태인지 추적
    public bool IsDialogueActive { get; private set; } = false;

    void Awake() { Instance = this; }

    public void StartDialogue(DialogueData data, DialogueNPC npc)
    {
        currentData = data;
        currentNodeIndex = data.startNodeIndex;
        currentNPC = npc; 

        IsDialogueActive = true; 

        // NullReferenceException 방지를 위한 안전 로직 (DialogueUI.Instance가 설정된다고 가정)
        if (DialogueUI.Instance == null)
        {
            DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include); 
            if (ui != null)
            {
                DialogueUI.Instance = ui; 
            }
            else
            {
                Debug.LogError("[DialogueManager] DialogueUI 오브젝트를 씬에서 찾을 수 없습니다. UI를 확인할 수 없습니다.");
                IsDialogueActive = false;
                return;
            }
        }

        DialogueUI.Instance.Show();
        ShowNode(currentNodeIndex);
    }

    /// <summary>
    /// 퀘스트 제출 UI를 띄우고 대화를 종료하는 함수. DialogueUI에서 텍스트 출력 완료 후 호출됩니다.
    /// </summary>
    public void StartQuestSubmission(QuestData questData)
    {
        // 🚨🚨 1. currentData Null 체크 추가 (오류 방지) 🚨🚨
        if (currentData == null || currentData.nodes == null)
        {
            Debug.LogError("[DialogueManager] currentData가 Null이거나 노드 배열이 Null입니다. 퀘스트 납입을 시작할 수 없습니다.");
            IsDialogueActive = false;
            return;
        }

        Debug.Log($"[DialogueManager] QUEST CALL: 퀘스트 '{questData.questName}' 시작 요청. 대화 종료 및 제출 UI 활성화 시도.");
        
        // 1. 대화 UI 닫기
        EndDialogue();
        
        // 2. 퀘스트 관리자에게 제출 창을 열도록 명령 (Instance 사용 재차 확인)
        if (QuestManager.instance != null) // QuestManager 싱글톤 변수가 instance (소문자)로 정의되어 있다고 가정
        {
            // 수정된 로직: currentData와 currentNodeIndex를 사용
            if (currentData.nodes.Length > currentNodeIndex)
            {
                // 이 인덱스는 퀘스트 납입 성공 후 이어갈 대화 노드 인덱스입니다.
                int nextIndexAfterSubmission = currentData.nodes[currentNodeIndex].nextNodeIndex; 

                // OpenSubmitUI에 QuestData와 다음 노드 인덱스를 전달
                QuestManager.instance.OpenSubmitUI(questData, nextIndexAfterSubmission);
            }
            else
            {
                Debug.LogError("[DialogueManager] 현재 노드 인덱스가 유효하지 않습니다. 퀘스트 완료 후 대화가 이어지지 않습니다.");
                QuestManager.instance.OpenSubmitUI(questData, -1); // -1을 전달하여 대화 재개 시도 안 함
            }
        }
        else
        {
            Debug.LogError("[DialogueManager] QuestManager.instance가 Null입니다. QuestManager가 씬에 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// QuestManager가 퀘스트 납입 완료 후 호출하여 대화를 재개하는 함수.
    /// </summary>
    /// <param name="nodeIndex">재개할 대화 노드의 인덱스 (QuestManager에서 저장된 값)</param>
    public void ContinueDialogueAtNode(int nodeIndex)
    {
        if (currentData == null) 
        {
            // 이 시점에서는 이미 대화가 끝났다가 돌아온 것이므로, Null이 아닐 가능성이 높지만 방어합니다.
            Debug.LogError("[DialogueManager] 현재 활성화된 DialogueData가 없어 대화를 재개할 수 없습니다. NPC와의 대화를 다시 시작해야 합니다.");
            return;
        }
        
        // 대화 UI 다시 보여주기
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Show();
        }
        
        // 지정된 노드로 대화 진행
        currentNodeIndex = nodeIndex;
        ShowNode(currentNodeIndex);
        
        IsDialogueActive = true;
        Debug.Log($"[DialogueManager] 퀘스트 완료 후 노드 {nodeIndex}에서 대화를 재개합니다.");
    }
    
    public void ShowNode(int nodeIndex)
    {
        if (currentData == null || currentData.nodes == null || nodeIndex < 0 || nodeIndex >= currentData.nodes.Length)
        {
            Debug.LogError($"[DialogueManager] Invalid node index or currentData is null: {nodeIndex}");
            EndDialogue();
            return;
        }

        var node = currentData.nodes[nodeIndex];
        if (DialogueUI.Instance != null) 
        { 
            DialogueUI.Instance.DisplayNode(node);
        }
    }

    /// <summary>
    /// 선택지 클릭 등으로 다음 노드로 이동 요청. 퀘스트 체크 로직은 DialogueUI로 이동했습니다.
    /// </summary>
    public void GoToNextNode(int index)
    {
        // -1은 대화 종료를 의미
        if (index < 0) 
        { 
            EndDialogue(); 
            return; 
        }

        // 다음 노드로 이동
        currentNodeIndex = index;
        ShowNode(currentNodeIndex);
    }

    public void EndDialogue()
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Hide();
        }
        
        // currentData = null; // 대화 재개를 위해 이 부분을 주석 처리합니다.
        // QuestManager가 대화를 재개해야 하므로, currentData를 유지해야 합니다.

        // 대화 종료 시 NPC 호출
        if (currentNPC != null)
        {
            currentNPC.OnDialogueEnd();
            currentNPC = null;
        }

        IsDialogueActive = false;
    }
}