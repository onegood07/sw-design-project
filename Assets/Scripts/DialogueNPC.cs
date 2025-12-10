using UnityEngine;


public class DialogueNPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogueData; 

    // ⭐ [추가]: 퀘스트 완료 상태 및 반복 노드 필드
    [Header("Quest Status")]
    // private set을 사용하여 외부에서 임의로 변경하는 것을 방지합니다.
    public bool IsQuestCompleted { get; private set; } = false;
    public int RepeatDialogueNodeIndex { get; private set; } = -1;
    
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
        // DialogueManager 인스턴스 확인
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[DialogueNPC] DialogueManager 인스턴스를 찾을 수 없습니다.");
            return;
        }
        
        // 🚨 이미 대화 중이라면 상호작용 무시
        if (DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        int startNode;
        
        if (IsQuestCompleted && RepeatDialogueNodeIndex != -1)
        {
            // ⭐ 퀘스트가 완료되었고 반복 노드가 설정되어 있다면, 반복 노드를 시작 노드로 사용
            startNode = RepeatDialogueNodeIndex;
            Debug.Log($"{gameObject.name}: 퀘스트 완료 상태. 반복 노드({startNode})에서 대화 시작.");
        }
        else
        {
            // ⭐ 기본 대화 시작 노드 사용
            startNode = dialogueData.startNodeIndex;
            Debug.Log($"{gameObject.name}: 대화 시작 노드({startNode})에서 대화 시작.");
        }

        // DialogueManager.StartDialogue(DialogueData, NPC, StartNodeIndex) 시그니처가 
        // DialogueManager.cs에 구현되어 있다고 가정하고 호출합니다.
        DialogueManager.Instance.StartDialogue(dialogueData, this, startNode); 
    }

    /// <summary>
    /// 대화 종료 시 호출됩니다. (DialogueManager에서 호출)
    /// </summary>
    public void OnDialogueEnd()
    {
        // DialogueManager.EndDialogue 내부에서 GameManager.EndInteraction() 호출
    }
    
    // ──────────────────────────────────────────────
    // MARK: - Quest Status Management
    // ──────────────────────────────────────────────

    /// <summary>
    /// 퀘스트 완료 후 NPC의 상태를 영구적으로 변경하고 대화 반복 노드를 설정
    /// </summary>
    public void CompleteQuestState(int repeatNodeIndex)
    {
        IsQuestCompleted = true;
        RepeatDialogueNodeIndex = repeatNodeIndex;
        
        Debug.Log($"[DialogueNPC] {gameObject.name}: 퀘스트 완료. 반복 노드 {repeatNodeIndex}로 설정됨.");
        
        // ⭐ 1. SpawnManager에게 이 NPC의 영구 데이터를 업데이트하도록 요청
        // 이 부분은 퀘스트 영구화 로직이므로 유지됩니다.
        SpawnManager.Instance?.UpdatePersistentNPCData(gameObject, repeatNodeIndex, true); 
        
        
        // ❌ 2. [오류 발생 코드 제거]: 인벤토리 호출 코드를 제거합니다.
        /*
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ToggleInventory(true); 
            Debug.Log("[DialogueNPC] 퀘스트 완료 후 InventoryManager.ToggleInventory(true) 호출 완료.");
        }
        */
        
        // 💡 [다음 단계]: 인벤토리 UI를 열기 위한 정확한 함수를 호출해야 합니다.
        if (InventoryManager.Instance != null)
        {
            // InventoryManager에 정의된 실제 인벤토리 오픈/토글 함수를 대신 호출해야 합니다.
            // 예를 들어, InventoryManager.Instance.OpenInventory();
        }
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