using UnityEngine;


public class DialogueNPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogueData; 

    [Header("Quest Status")]
    public bool IsQuestCompleted { get; private set; } = false;
    public int RepeatDialogueNodeIndex { get; private set; } = -1;

    [Header("Shelter / Survivor")]
    public bool DisappearAfterDialogue { get; private set; } = false;
    
    // ⭐ [추가]: SpawnManager를 위한 복원/상태 저장용 ID (옵션)
    [HideInInspector] public string PersistentID;


    void Start()
    {
        // Start 시점에 퀘스트 상태 복원은 SpawnManager의 RestorePersistentObjects에서 직접 호출합니다.
    }

    /// <summary>
    /// 플레이어가 상호작용할 때 호출됩니다. (IInteractable 구현)
    /// </summary>
    public void OnInteract()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[DialogueNPC] DialogueManager 인스턴스를 찾을 수 없습니다.");
            return;
        }
        
        if (DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        int startNode;
        
        if (IsQuestCompleted && RepeatDialogueNodeIndex != -1)
        {
            startNode = RepeatDialogueNodeIndex;
            Debug.Log($"{gameObject.name}: 퀘스트 완료 상태. 반복 노드({startNode})에서 대화 시작.");
        }
        else
        {
            startNode = dialogueData.startNodeIndex;
            Debug.Log($"{gameObject.name}: 대화 시작 노드({startNode})에서 대화 시작.");
        }

        DialogueManager.Instance.StartDialogue(dialogueData, this, startNode); 
    }

    public void OnDialogueEnd()
    {
        if (DisappearAfterDialogue)
        {
            gameObject.SetActive(false);
            Debug.Log($"[DialogueNPC] {gameObject.name}: 대화 종료와 함께 쉘터로 이동 처리(필드에서 비활성화).");
        }
    }
    
    // ──────────────────────────────────────────────
    // MARK: - Quest Status Management
    // ──────────────────────────────────────────────

    public void CompleteQuestState(int repeatNodeIndex)
    {
        IsQuestCompleted = true;
        RepeatDialogueNodeIndex = repeatNodeIndex;
        
        Debug.Log($"[DialogueNPC] {gameObject.name}: 퀘스트 완료. 반복 노드 {repeatNodeIndex}로 설정됨.");
        
        SpawnManager.Instance?.UpdatePersistentNPCData(gameObject, repeatNodeIndex, true); 
        // (인벤토리 관련 주석/임시 코드 제거)
    }

    public void MarkDisappearAfterDialogue()
    {
        DisappearAfterDialogue = true;
        Debug.Log($"[DialogueNPC] {gameObject.name}: 대화 종료 후 필드에서 숨김 예정으로 표시됨.");
    }

    /// <summary>
    /// SpawnManager가 씬 로드 시 호출하여 이전 상태를 복원합니다.
    /// </summary>
    public void RestoreState(bool isCompleted, int repeatNode)
    {
        IsQuestCompleted = isCompleted;
        RepeatDialogueNodeIndex = repeatNode;
        
        if (IsQuestCompleted)
        {
            Debug.Log($"[DialogueNPC] {gameObject.name}: 상태 복원 완료. 퀘스트 완료됨, 반복 노드: {RepeatDialogueNodeIndex}.");
        }
    }
}