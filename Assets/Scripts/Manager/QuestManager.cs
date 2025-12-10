using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI Root")]
    public GameObject submitCanvas; // Hierarchy의 SubmitCanvas 연결
    
    [Header("UI 계층 구조")]
    public GameObject QuestPanel;   // SubmitCanvas의 자식 QuestPanel 연결
    public GameObject submitImagePanel; // QuestPanel의 자식 SubmitImage 연결
    
    [Header("UI 요소")]
    public Image requiredItemImageComponent;
    public Text submitAmountText;
    public Button submitButton;
    // QuestSlot 클래스가 Item 타입의 RewardItem과 int 타입의 RewardCount 필드를 가지고 있다고 가정
    public QuestSlot questDropSlot; 
    
    private QuestData activeQuestData;
    private int nextDialogueNodeIndex = -1;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (submitCanvas != null)
            submitCanvas.SetActive(false);
    }

    // ───────────────────────────────
    // OpenSubmitUI: UI 열기
    public void OpenSubmitUI(QuestData data, int nextIndex)
    {
        Debug.Log($"[OpenSubmitUI] 호출: data={(data != null ? data.questName : "null")}, nextIndex={nextIndex}");
        
        if (data == null) return;

        activeQuestData = data;
        nextDialogueNodeIndex = nextIndex;

        // UI 강제 초기화 및 데이터 설정
        if (questDropSlot != null)
        {
            questDropSlot.ResetSlot();
            questDropSlot.SetupSlot(data);
            questDropSlot.gameObject.SetActive(true);
        }

        if (submitAmountText != null)
        {
            submitAmountText.gameObject.SetActive(true);
            int submitted = questDropSlot != null ? questDropSlot.submittedCount : 0;
            int required = questDropSlot != null ? questDropSlot.RequiredAmount : 0;
            submitAmountText.text = $"수량: {submitted} / {required}";
        }

        if (requiredItemImageComponent != null)
        {
            requiredItemImageComponent.sprite = data.requiredItemIcon;
            requiredItemImageComponent.enabled = data.requiredItemIcon != null;
            requiredItemImageComponent.color = Color.white;
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
            submitButton.gameObject.SetActive(true);
        }

        // 1. SubmitCanvas 및 패널 활성화
        if (submitCanvas != null) submitCanvas.SetActive(true);
        if (QuestPanel != null) QuestPanel.SetActive(true);
        
        // 3. SubmitImagePanel 활성화
        if (submitImagePanel != null)
            submitImagePanel.SetActive(true);
        else
            Debug.LogError("[QuestManager] SubmitImagePanel이 Inspector에 연결되지 않았습니다. UI 콘텐츠가 보이지 않습니다.");

        // 4. 인벤토리 UI 활성화 
        if (InventoryUI.instance != null) 
        {
            // InventoryUI.instance.OpenInventory()는 정의되어 있다고 가정합니다.
            InventoryUI.instance.OpenInventory(); 
            Debug.Log("[QuestManager] 퀘스트 제출 UI와 함께 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[QuestManager] InventoryUI 인스턴스를 찾을 수 없습니다. 인벤토리를 열 수 없습니다.");
        }
        // ⭐ 퀘스트 UI가 열릴 때 상호작용 시작을 알립니다. (DialogueManager가 EndDialogue를 호출하지 않았으므로 상호작용 상태 유지)
        GameManager.Instance?.StartInteraction();
        Debug.Log("[OpenSubmitUI] UI 열림 완료");
    }

// ───────────────────────────────
// 제출 버튼 클릭
public void OnSubmitButtonClicked()
{
    if (activeQuestData == null)
    {
        Debug.LogWarning("[QuestManager] OnSubmitButtonClicked: activeQuestData가 Null입니다. 제출 중단.");
        return;
    }
    
    // 1. 초기 Null 체크
    if (questDropSlot == null)
    {
        Debug.LogError("[QuestManager] OnSubmitButtonClicked: questDropSlot 참조가 Null입니다! Inspector 연결 확인.");
        return;
    }

    // QuestSlot.ConfirmSubmission()는 정의되어 있다고 가정합니다.
    bool confirmed = questDropSlot.ConfirmSubmission(); 
    
    // 2. ConfirmSubmission() 호출 후, QuestSlot이 혹시 파괴되었는지 재확인 (방어 코드)
    if (questDropSlot == null) 
    {
         Debug.LogError("[QuestManager] ConfirmSubmission 호출 후 QuestSlot이 파괴되었습니다. CloseSubmitUI 호출 시점을 확인하십시오.");
         return;
    }

    if (confirmed)
    {
        Debug.Log("[OnSubmitButtonClicked] 아이템 제출 성공 - ConfirmSubmission() 성공.");
        
        // 퀘스트 슬롯의 상태를 다시 확인하여 완료 여부 판단
        if (questDropSlot.submittedCount >= questDropSlot.RequiredAmount)
        {
             HandleQuestCompletion(questDropSlot);
             
             // 퀘스트 완료 후, 대화 재개 로직이 끝난 후 UI를 닫습니다.
             CloseSubmitUI(); 
        }
        else
        {
             // 제출은 성공했지만 아직 미완료
             if (submitAmountText != null)
                submitAmountText.text = $"수량: {questDropSlot.submittedCount} / {questDropSlot.RequiredAmount}";
        }
    }
}
    
// ───────────────────────────────
// 아이템 제출 시 (QuestSlot에서 호출)
public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
{
    // HandleQuestCompletion 호출은 OnSubmitButtonClicked으로 통합되어 중복 보상 문제를 해결했습니다.
    
    if (slot.submittedCount >= slot.RequiredAmount)
    {
        Debug.Log($"[OnItemSubmitted] 퀘스트 '{activeQuestData.questName}' 완료 상태가 되었습니다. (보상 지급 대기)");
    }
    
    if (submitAmountText != null)
        submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
}

// ───────────────────────────────
// UI 닫기 (X 버튼 클릭 또는 완료 후 호출)

public void CloseSubmitUI()
{
    Debug.Log("[CloseSubmitUI] 호출됨. 제출 UI와 대화 상태 정리 시작.");

    // ⭐ 퀘스트 미완료 상태 (사용자가 X 버튼 등으로 닫음)일 때만 대화를 취소하고 상호작용 종료
    if (activeQuestData != null) 
    {
        if (DialogueManager.Instance != null)
        {
            // EndDialogue() 내부에서 GameManager.EndInteraction() 호출
            DialogueManager.Instance.EndDialogue(); 
            Debug.Log("[CloseSubmitUI] 퀘스트 미완료 상태에서 UI 닫힘: DialogueManager.EndDialogue() 호출.");
        } else {
             Debug.LogError("[CloseSubmitUI Error] DialogueManager.Instance가 Null입니다! 상호작용 종료 수동 호출.");
             GameManager.Instance?.EndInteraction(); // DialogueManager가 없으면 수동 종료
        }
    }
    // 퀘스트 완료 후 닫는 경우: HandleQuestCompletion에서 activeQuestData = null로 설정됨.
    // 대화 재개 여부에 따라 상호작용 종료를 결정합니다.
    if (activeQuestData == null && DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive)
    {
        // 퀘스트는 완료되었는데 대화가 재개되지 않았을 경우 (HandleQuestCompletion에서 nextDialogueNodeIndex == -1인 경우)
        GameManager.Instance?.EndInteraction();
        Debug.Log("[CloseSubmitUI] 퀘스트 완료 후 대화 미재개: 상호작용 수동 종료.");
    }
    // DialogueManager가 없는데 activeQuestData가 null인 경우 (에러 상황), EndInteraction 호출은 생략합니다.

    // 4. 인벤토리 UI 비활성화
    if (InventoryUI.instance != null) 
    {
        // InventoryUI.instance.CloseInventory()는 정의되어 있다고 가정합니다.
        InventoryUI.instance.CloseInventory();
        Debug.Log("[CloseSubmitUI] 인벤토리 UI를 닫았습니다.");
    } else {
         Debug.LogWarning("[CloseSubmitUI] InventoryUI.instance가 Null이므로 인벤토리 닫기를 건너뜁니다.");
    }
    
    // 1~3. UI 비활성화
    if (submitImagePanel != null) submitImagePanel.SetActive(false);
    if (QuestPanel != null) QuestPanel.SetActive(false);
    
    // **가장 상위 Canvas 비활성화**
    if (submitCanvas != null) {
        submitCanvas.SetActive(false); 
        Debug.Log("[CloseSubmitUI] SubmitCanvas 비활성화 완료.");
    } else {
        Debug.LogError("[CloseSubmitUI] submitCanvas가 Null입니다! UI가 닫히지 않는 근본 원인일 수 있습니다. Inspector를 확인하세요.");
    }
        
    if (requiredItemImageComponent != null) requiredItemImageComponent.enabled = false;
    if (questDropSlot != null)
    {
        questDropSlot.ResetSlot();
        questDropSlot.gameObject.SetActive(false); 
    }
    
    // 버튼 리스너 명시적 제거
    if (submitButton != null)
    {
        submitButton.onClick.RemoveAllListeners();
        Debug.Log("[CloseSubmitUI] SubmitButton 리스너를 모두 제거했습니다.");
    }
    activeQuestData = null; // UI가 닫힐 때 완전히 초기화
    nextDialogueNodeIndex = -1;

    Debug.Log("[CloseSubmitUI] 상태 초기화 완료");
}

// 퀘스트 완료 후 보상 지급 및 상태 정리
private void HandleQuestCompletion(QuestSlot slot)
{
    QuestData data = slot.AssignedQuestData;
    
    // 퀘스트 완료 후 NPC가 반복할 대화 노드의 인덱스 (임시 지정)
    const int QUEST_COMPLETED_REPEAT_NODE_INDEX = 0; 

    if (data == null)
    {
        Debug.LogError("[HandleQuestCompletion] QuestData를 찾을 수 없습니다. 보상 지급 실패.");
        return;
    }
    
    Debug.Log($"[HandleQuestCompletion] 보상 지급 시작: {data.questName}");

    // 1. 아이템 보상 처리 (생략)
    // 2. 생존자 증가 보상 처리 (생략)
    if (data.increaseSurvivors)
    {
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.AddSurvivors(data.survivorIncreaseAmount);
            Debug.Log($"[HandleQuestCompletion] 생존자 수 증가 보상 지급 완료: {data.survivorIncreaseAmount}명 증가.");
        }
        else
        {
             Debug.LogError("[HandleQuestCompletion Error] GameManager.Instance가 Null입니다! 생존자 보상 지급 실패.");
        }
    }
    
    // ⭐ [추가]: NPC에게 퀘스트 완료 상태를 영구적으로 설정
    if (DialogueManager.Instance != null && DialogueManager.Instance.currentNPC != null) 
    {
        DialogueNPC currentNPC = DialogueManager.Instance.currentNPC;
        
        // DialogueNPC의 CompleteQuestState를 호출하여 NPC 자신의 상태를 변경하고,
        // 이 함수 내부에서 SpawnManager에게 영구 데이터 업데이트를 요청합니다.
        // DialogueNPC.CompleteQuestState(int repeatNodeIndex)는 DialogueNPC.cs에 구현되어 있다고 가정합니다.
        currentNPC.CompleteQuestState(QUEST_COMPLETED_REPEAT_NODE_INDEX); 
        Debug.Log($"[HandleQuestCompletion] NPC '{currentNPC.name}'의 퀘스트 완료 상태 설정 완료. 반복 노드: {QUEST_COMPLETED_REPEAT_NODE_INDEX}");
    }


    // 3. 대화 재개 및 상태 초기화 로직
    if (nextDialogueNodeIndex != -1)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ContinueDialogueAtNode(nextDialogueNodeIndex);
            Debug.Log($"[HandleQuestCompletion] 다음 노드 진행: {nextDialogueNodeIndex}");
        } 
        else
        {
             Debug.LogError("[HandleQuestCompletion Error] DialogueManager.Instance가 Null입니다! 대화 재개 생략.");
        }
    } 
    else
    {
         Debug.LogWarning("[HandleQuestCompletion] 다음 노드 인덱스가 없습니다. 상호작용 종료를 CloseSubmitUI에 위임합니다.");
    }

    activeQuestData = null; 
    nextDialogueNodeIndex = -1;
    Debug.Log("[HandleQuestCompletion] 상태 초기화 완료");
}

}