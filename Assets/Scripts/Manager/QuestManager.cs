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
            InventoryUI.instance.OpenInventory();
            Debug.Log("[QuestManager] 퀘스트 제출 UI와 함께 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[QuestManager] InventoryUI 인스턴스를 찾을 수 없습니다. 인벤토리를 열 수 없습니다.");
        }
        GameManager.Instance?.StartInteraction();
        Debug.Log("[OpenSubmitUI] UI 열림 완료");
    }

// ───────────────────────────────
// 제출 버튼 클릭
public void OnSubmitButtonClicked()
{
    // (줄 110)
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

    bool confirmed = questDropSlot.ConfirmSubmission(); // (A) ConfirmSubmission 호출
    
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
            //  Debug.Log($"[OnSubmitButtonClicked] 퀘스트 '{activeQuestData.questName}' 완료!");
             HandleQuestCompletion(questDropSlot);
             
             // 💡 퀘스트 완료 후, 대화 재개 로직이 끝난 후 UI를 닫습니다.
             // OnItemSubmitted에서 CloseSubmitUI를 제거했으므로 여기서 닫아줍니다.
             CloseSubmitUI(); 
        }
        else
        {
             if (submitAmountText != null)
                submitAmountText.text = $"수량: {questDropSlot.submittedCount} / {questDropSlot.RequiredAmount}";
        }
    }
}
    
// ───────────────────────────────
// 아이템 제출 시 (QuestSlot에서 호출)
public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
{
    // 여기서 CloseSubmitUI() 호출을 제거합니다. (OnSubmitButtonClicked에서 최종 처리)
    if (slot.submittedCount >= slot.RequiredAmount)
    {
        Debug.Log($"[OnItemSubmitted] 퀘스트 '{activeQuestData.questName}' 완료!");
        // HandleQuestCompletion(slot);
    }
    else
    {
        if (submitAmountText != null)
            submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
    }
}

// ───────────────────────────────
// UI 닫기 (X 버튼 클릭 또는 완료 후 호출)

public void CloseSubmitUI()
{
    Debug.Log("[CloseSubmitUI] 호출됨. 제출 UI와 대화 상태 정리 시작.");

    // 🚨 핵심 로직: 퀘스트 완료 후 대화 재개가 예정되었는지 확인합니다.
    // HandleQuestCompletion에서 초기화하지 않았기 때문에 이 값이 남아있다면 대화 재개 예정입니다.
    bool dialogueIsResuming = nextDialogueNodeIndex != -1;

    // 1. 대화 관리자 정리
    // 퀘스트가 미완료 상태(activeQuestData != null) 이고, 대화 재개 예정이 아닐 때만 Dialogue를 종료합니다.
    if (activeQuestData != null && !dialogueIsResuming) 
    {
        if (DialogueManager.Instance != null)
        {
            // 미완료 상태에서 닫으면, 현재 대화 상태를 취소합니다. (EndDialogue가 EndInteraction을 호출할 것입니다.)
            DialogueManager.Instance.EndDialogue(); 
            Debug.Log("[CloseSubmitUI] 퀘스트 취소 (미완료): DialogueManager.EndDialogue() 호출.");
        } else {
             Debug.LogError("[CloseSubmitUI Error] DialogueManager.Instance가 Null입니다! 상호작용 종료 수동 호출.");
             GameManager.Instance?.EndInteraction();
        }
    }
    // ⭐⭐ 퀘스트 완료 후 대화 재개 중일 때는 EndDialogue() 호출을 건너뛰어 대화가 진행되도록 합니다. ⭐⭐

    // 2. GameManager.EndInteraction 호출 로직 (좀비 이동/공격 방지 해제)
    // 다음 대화 노드가 없을 때만 상호작용을 종료합니다.
    if (GameManager.Instance != null && !dialogueIsResuming) 
    {
        // 퀘스트 완료 후 대화가 재개되지 않을 경우 (HandleQuestCompletion에서 nextDialogueNodeIndex == -1이었을 경우)
        // 또는 미완료 상태에서 닫았을 경우 (EndDialogue가 EndInteraction을 호출했거나, 여기서 수동 호출)
        
        // 미완료 상태에서 닫았을 때 EndDialogue가 EndInteraction을 이미 호출했을 수 있지만,
        // 안전하게 activeQuestData == null일 때만 여기서 EndInteraction을 최종적으로 확인합니다.
        if (activeQuestData == null)
        {
             GameManager.Instance.EndInteraction();
             Debug.Log("[CloseSubmitUI] 퀘스트 완료 후 대화 미재개: 상호작용 수동 종료.");
        }
    }
    else if (dialogueIsResuming)
    {
         Debug.Log("[CloseSubmitUI] 퀘스트 완료 후 대화 재개 예정이므로 상호작용 상태를 유지합니다.");
    }

    // 4. 인벤토리 UI 비활성화
    if (InventoryUI.instance != null) 
    {
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
    
    // ⭐⭐ 최종적으로 상태 변수를 클리어합니다. (가장 안전한 시점) ⭐⭐
    activeQuestData = null; 
    nextDialogueNodeIndex = -1;

    Debug.Log("[CloseSubmitUI] 상태 초기화 완료");
}

// 퀘스트 완료 후 보상 지급 및 상태 정리
private void HandleQuestCompletion(QuestSlot slot)
{
    QuestData data = slot.AssignedQuestData;
    
    if (data == null)
    {
        Debug.LogError("[HandleQuestCompletion] QuestData를 찾을 수 없습니다. 보상 지급 실패.");
        return;
    }
    
    Debug.Log($"[HandleQuestCompletion] 보상 지급 시작: {data.questName}");

    // 1. 아이템 보상 처리 (로직 복구)
    if (slot.RewardItem != null)
    {
        // InventoryManager 인스턴스 확인 후 보상 지급
        if (InventoryManager.Instance != null)
        {
            // 사용자의 InventoryManager 클래스에 맞춰 적절한 보상 추가 함수를 호출해야 합니다.
            // 여기서는 표준적인 함수명을 사용했습니다.
            InventoryManager.Instance.AddRewardItemToInventory(slot.RewardItem, slot.RewardCount); 
            
            Debug.Log($"[HandleQuestCompletion] 아이템 보상 지급 완료: {slot.RewardItem.itemName} x{slot.RewardCount}");
        }
        else
        {
            // InventoryManager 클래스가 없다면 이 부분에서 에러가 발생합니다.
            Debug.LogError("[HandleQuestCompletion Error] InventoryManager 인스턴스가 Null입니다. 보상 지급 실패.");
        }
    }
    
    // 2. 생존자 증가 보상 처리 (로직 유지)
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
    
    // 3. 대화 재개 및 상호작용 유지 로직
    if (nextDialogueNodeIndex != -1)
    {
        if (DialogueManager.Instance != null)
        {
            // ⭐⭐⭐ 핵심 수정: 대화 재개 전에 상호작용 상태를 다시 확정합니다. ⭐⭐⭐
            GameManager.Instance?.StartInteraction(); 
            Debug.Log("[HandleQuestCompletion] 대화 재개를 위해 GameManager.StartInteraction() 호출.");
            
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

    // 🚨🚨🚨 이전의 activeQuestData = null; 및 nextDialogueNodeIndex = -1; 초기화 코드는 CloseSubmitUI로 옮겨 최종 정리합니다.
    Debug.Log("[HandleQuestCompletion] 보상 지급 및 대화 명령 완료.");
}
}