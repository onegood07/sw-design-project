using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData; // 현재 DialogueData
    private int currentNodeIndex; // 현재 노드 인덱스
    public DialogueNPC currentNPC;
    
    public bool IsDialogueActive { get; private set; } = false;

    void Awake() 
    { 
        // 싱글톤 패턴 보강 (선택적)
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    /// <summary>
    /// 대화를 시작하는 함수. DialogueNPC가 계산한 시작 노드 인덱스를 받습니다.
    /// </summary>
    public void StartDialogue(DialogueData data, DialogueNPC npc, int startingIndex) // ⭐ 시그니처 수정
    {
        Debug.Log($"[DialogueManager] StartDialogue 호출 - NPC: {npc.name}, startingIndex: {startingIndex}");

        currentData = data;
        currentNodeIndex = startingIndex; // ⭐ 전달받은 시작 인덱스 사용
        currentNPC = npc; 

        IsDialogueActive = true; 

        // 1. ⭐ [GameManager 상호작용 시작] 대화 시작 시 상호작용 시작.
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
                // 상호작용 시작 실패 시 종료 상태로 되돌립니다.
                GameManager.Instance?.EndInteraction();
                return;
            }
        }

        DialogueUI.Instance.Show();
        ShowNode(currentNodeIndex);
    }

    /// <summary>
    /// 퀘스트 제출 UI를 띄우고 대화를 일시 중단하는 함수 (EndDialogue 호출 안 함!)
    /// </summary>
    public void StartQuestSubmission(QuestData questData)
    {
        // 이 함수는 기존 로직을 유지합니다. currentNodeIndex는 현재 대화 위치입니다.
        Debug.Log($"[DialogueManager] StartQuestSubmission 호출 - currentData: {currentData}, currentNodeIndex: {currentNodeIndex}");

        if (currentData == null || currentData.nodes == null)
        {
            Debug.LogWarning("[DialogueManager] currentData가 Null이거나 노드 배열이 Null입니다. 기본 인덱스 0으로 SubmitUI 시도");
            QuestManager.instance?.OpenSubmitUI(questData, 0);
            IsDialogueActive = false;
            return;
        }

        // 1. 대화 UI만 숨김 (대화 데이터는 유지)
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Hide(); 

        // 2. 대화 상태를 '비활성'으로 전환 (UI가 보이지 않으므로)
        IsDialogueActive = false;

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

        // 3. 퀘스트 제출 UI를 띄울 때 돌아올 인덱스를 계산하여 전달
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
            // 퀘스트 완료 후 대화 재개 실패 시 상호작용 종료 (방어 코드)
            GameManager.Instance?.EndInteraction();
            return;
        }

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show();

        currentNodeIndex = nodeIndex;
        ShowNode(currentNodeIndex);

        IsDialogueActive = true;
        // StartDialogue에서 이미 StartInteraction을 호출했습니다.
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

        // 대화가 완전히 끝났으므로 모든 상태 초기화
        currentData = null; // 대화 데이터 초기화
        currentNodeIndex = -1;

        if (currentNPC != null)
            currentNPC.OnDialogueEnd(); // NPC에게 종료 알림

        currentNPC = null;
        IsDialogueActive = false;
        
        // 2. ⭐ [GameManager 상호작용 종료] 대화가 완전히 끝났을 때 상호작용 종료를 알립니다.
        GameManager.Instance?.EndInteraction();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecuteReservedSurvivorIncrease();
        }

        Debug.Log("[DialogueManager] 대화 상태 완전 초기화 완료.");
    }
}