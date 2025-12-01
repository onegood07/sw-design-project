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

        // NullReferenceException 방지를 위한 안전 로직 (경고 CS0618 수정됨)
        if (DialogueUI.Instance == null)
        {
            // FindObjectOfType 대신 FindAnyObjectByType을 사용하여 경고 제거
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
        Debug.Log($"[DialogueManager] QUEST CALL: 퀘스트 '{questData.questName}' 시작 요청. 대화 종료 및 제출 UI 활성화 시도.");
        
        // 1. 대화 UI 닫기
        EndDialogue();
        
        // 2. 퀘스트 관리자에게 제출 창을 열도록 명령
        if (QuestManager.instance != null)
        {
            // QuestManager의 OpenSubmitUI 함수 호출
            QuestManager.instance.OpenSubmitUI(questData);
        }
        else
        {
            Debug.LogError("[DialogueManager] QuestManager.instance가 Null입니다. QuestManager가 씬에 있는지 확인하세요.");
        }
    }

    public void ShowNode(int nodeIndex)
    {
        if (currentData == null || currentData.nodes == null || nodeIndex < 0 || nodeIndex >= currentData.nodes.Length)
        {
            Debug.LogError($"[DialogueManager] Invalid node index: {nodeIndex}");
            EndDialogue();
            return;
        }

        var node = currentData.nodes[nodeIndex];
        if (DialogueUI.Instance == null) { return; } 

        DialogueUI.Instance.DisplayNode(node);
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

        // ⭐ 핵심: 퀘스트 체크 로직 제거. DialogueUI가 텍스트 출력 후 퀘스트를 체크합니다.

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
        
        currentData = null;

        // 대화 종료 시 NPC 호출
        if (currentNPC != null)
        {
            currentNPC.OnDialogueEnd();
            currentNPC = null;
        }

        IsDialogueActive = false;
    }
}