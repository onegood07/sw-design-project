using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData; // 현재 DialogueData
    private int currentNodeIndex; // 현재 노드 인덱스
    private DialogueNPC currentNPC; // 현재 NPC
    
    public bool IsDialogueActive { get; private set; } = false;

    void Awake() 
    { 
        Instance = this; 
    }

    public void StartDialogue(DialogueData data, DialogueNPC npc)
    {
        Debug.Log($"[DialogueManager] StartDialogue 호출 - NPC: {npc.name}, startNodeIndex: {data.startNodeIndex}");

        currentData = data;
        currentNodeIndex = data.startNodeIndex;
        currentNPC = npc; 

        IsDialogueActive = true; 

        GameManager.Instance?.StartInteraction();

        if (DialogueUI.Instance == null)
        {
            DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include); 
            if (ui != null)
            {
                DialogueUI.Instance = ui; 
            }
            else
            {
                Debug.LogError("[DialogueManager] DialogueUI 오브젝트를 씬에서 찾을 수 없습니다.");
                IsDialogueActive = false;
                return;
            }
        }

        DialogueUI.Instance.Show();
        ShowNode(currentNodeIndex);
    }

    /// <summary>
    /// 퀘스트 제출 UI를 띄우고 대화를 종료하는 함수
    /// </summary>
    public void StartQuestSubmission(QuestData questData)
    {
        Debug.Log($"[DialogueManager] StartQuestSubmission 호출 - currentData: {currentData}, currentNodeIndex: {currentNodeIndex}");

        if (currentData == null || currentData.nodes == null)
        {
            Debug.LogWarning("[DialogueManager] currentData가 Null이거나 노드 배열이 Null입니다. 기본 인덱스 0으로 SubmitUI 시도");
            QuestManager.instance?.OpenSubmitUI(questData, 0);
            IsDialogueActive = false;
            return;
        }

        // 디버깅용: 현재 노드 정보 출력
        if (currentNodeIndex >= 0 && currentNodeIndex < currentData.nodes.Length)
        {
            var node = currentData.nodes[currentNodeIndex];
            Debug.Log($"[DialogueManager] 현재 노드 - index: {currentNodeIndex}, nextNodeIndex: {node.nextNodeIndex}, text: {node.text}");
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] currentNodeIndex가 유효하지 않습니다: {currentNodeIndex}");
        }

        EndDialogue();

        int nextIndexAfterSubmission = 0;
        if (currentNodeIndex >= 0 && currentNodeIndex < currentData.nodes.Length)
            nextIndexAfterSubmission = currentData.nodes[currentNodeIndex].nextNodeIndex;

        Debug.Log($"[DialogueManager] QuestManager.OpenSubmitUI 호출, nextNodeIndex: {nextIndexAfterSubmission}");
        QuestManager.instance?.OpenSubmitUI(questData, nextIndexAfterSubmission);
    }

    /// <summary>
    /// QuestManager가 퀘스트 납입 완료 후 호출하여 대화를 재개
    /// </summary>
    public void ContinueDialogueAtNode(int nodeIndex)
    {
        Debug.Log($"[DialogueManager] ContinueDialogueAtNode 호출 - nodeIndex: {nodeIndex}");

        if (currentData == null) 
        {
            Debug.LogError("[DialogueManager] 현재 DialogueData가 없어 대화를 재개할 수 없습니다.");
            return;
        }

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show();

        currentNodeIndex = nodeIndex;
        ShowNode(currentNodeIndex);

        IsDialogueActive = true;
        GameManager.Instance?.StartInteraction();
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
        Debug.Log($"[DialogueManager] ShowNode - index: {nodeIndex}, text: {node.text}");

        if (DialogueUI.Instance != null) 
            DialogueUI.Instance.DisplayNode(node);
    }

    public void GoToNextNode(int index)
    {
        if (index < 0) 
        { 
            EndDialogue(); 
            return; 
        }

        currentNodeIndex = index;
        ShowNode(currentNodeIndex);
    }

    public void EndDialogue()
    {
        Debug.Log($"[DialogueManager] EndDialogue 호출 - currentNPC: {currentNPC?.name}");

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Hide();

        // currentData는 대화 재개를 위해 유지

        if (currentNPC != null)
        {
            currentNPC.OnDialogueEnd();
            currentNPC = null;
        }

        IsDialogueActive = false;
        GameManager.Instance?.EndInteraction();
    }
}