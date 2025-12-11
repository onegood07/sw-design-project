using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
            // ⭐ 초기 상태는 아이템이 없으므로 버튼 비활성화
            submitButton.interactable = false; 
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
    if (activeQuestData == null)
    {
        Debug.LogWarning("[QuestManager] OnSubmitButtonClicked: activeQuestData가 Null입니다. 제출 중단.");
        return;
    }
    
    if (questDropSlot == null)
    {
        Debug.LogError("[QuestManager] OnSubmitButtonClicked: questDropSlot 참조가 Null입니다! Inspector 연결 확인.");
        return;
    }

    // ⭐ 버튼이 활성화된 경우에만 ConfirmSubmission이 성공할 것입니다.
    bool confirmed = questDropSlot.ConfirmSubmission(); 
    
    if (confirmed)
    {
        Debug.Log("[OnSubmitButtonClicked] 아이템 제출 시도 성공.");
        
        // 퀘스트 슬롯의 상태를 다시 확인하여 완료 여부 판단
        if (questDropSlot.submittedCount >= questDropSlot.RequiredAmount)
        {
             HandleQuestCompletion(questDropSlot);
             CloseSubmitUI(); 
        }
        else
        {
             if (submitAmountText != null)
                 submitAmountText.text = $"수량: {questDropSlot.submittedCount} / {questDropSlot.RequiredAmount}";
             
             // 퀘스트가 미완료 상태라도 제출이 성공했으므로, 다시 버튼 비활성화 상태로 돌아갑니다.
             // (ConfirmSubmission 내부에서 ClearTemporarySlot()이 호출되고, 이는 NotifySlotStateChanged(0)를 호출합니다.)
        }
    }
    // confirmed가 false인 경우 (버튼이 활성화되었는데도 제출에 실패한 경우 - 매우 드묾), 아무 작업도 하지 않고 UI를 유지합니다.
}
    
// ───────────────────────────────
// 아이템 제출 시 (QuestSlot에서 호출 - 수량 갱신용)
public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
{
    if (slot.submittedCount >= slot.RequiredAmount)
    {
        Debug.Log($"[OnItemSubmitted] 퀘스트 '{activeQuestData.questName}' 완료 상태가 되었습니다. (제출 버튼 클릭 대기)");
    }
    
    // UI 업데이트 (ConfirmSubmission 직후)
    if (submitAmountText != null)
        submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
}

// ───────────────────────────────
// UI 닫기 (X 버튼 클릭 또는 완료 후 호출)

public void CloseSubmitUI()
{
    Debug.Log("[CloseSubmitUI] 호출됨. 제출 UI와 대화 상태 정리 시작.");

    bool questWasCancelled = (activeQuestData != null); 
    bool dialogueIsResuming = nextDialogueNodeIndex != -1;

    // 아이템 되돌리기 로직: 미완료 상태에서 닫았을 경우, 아이템을 돌려줍니다.
    if (questWasCancelled && questDropSlot != null)
    {
        questDropSlot.RollbackSubmission(); 
        Debug.Log("[CloseSubmitUI] 퀘스트 미완료 취소: 슬롯 아이템을 인벤토리로 되돌렸습니다. (임시 상태 해제)");
    }

    // 1. 대화 관리자 정리
    if (questWasCancelled && !dialogueIsResuming) 
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue(); 
            Debug.Log("[CloseSubmitUI] 퀘스트 취소 (미완료): DialogueManager.EndDialogue() 호출.");
        } else {
             Debug.LogError("[CloseSubmitUI Error] DialogueManager.Instance가 Null입니다! 상호작용 종료 수동 호출.");
             GameManager.Instance?.EndInteraction();
        }
    }
    
    // 2. GameManager.EndInteraction 호출 로직
    if (GameManager.Instance != null && !dialogueIsResuming) 
    {
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
    
    // 버튼 리스너 명시적 제거 및 최종 상태 정리
    if (submitButton != null)
    {
        submitButton.onClick.RemoveAllListeners();
        submitButton.interactable = false; // 혹시 몰라 최종 비활성화
        Debug.Log("[CloseSubmitUI] SubmitButton 리스너를 모두 제거했습니다.");
    }
    
    activeQuestData = null; 
    nextDialogueNodeIndex = -1;

    Debug.Log("[CloseSubmitUI] 상태 초기화 완료");
}

// 퀘스트 완료 후 보상 지급 및 상태 정리
private void HandleQuestCompletion(QuestSlot slot)
{
    // ... (보상 로직 생략, 기존과 동일) ...
    QuestData data = slot.AssignedQuestData;
    
    if (data == null)
    {
        Debug.LogError("[HandleQuestCompletion] QuestData를 찾을 수 없습니다. 보상 지급 실패.");
        return;
    }
    
    Debug.Log($"[HandleQuestCompletion] 보상 지급 시작: {data.questName}");

    // 1. 아이템 보상 처리
    if (slot.RewardItem != null)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddRewardItemToInventory(slot.RewardItem, slot.RewardCount); 
            
            Debug.Log($"[HandleQuestCompletion] 아이템 보상 지급 완료: {slot.RewardItem.itemName} x{slot.RewardCount}");
        }
        else
        {
            Debug.LogError("[HandleQuestCompletion Error] InventoryManager 인스턴스가 Null입니다. 보상 지급 실패.");
        }
    }
    
    // 2. 생존자 증가 보상 처리
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

    Debug.Log("[HandleQuestCompletion] 보상 지급 및 대화 명령 완료.");
}

// ───────────────────────────────
// [새로 추가] QuestSlot에서 호출되어 버튼 상태를 업데이트하는 함수
// ───────────────────────────────

/// <summary>
/// QuestSlot으로부터 임시 배치된 아이템의 수량 변경 알림을 받고 제출 버튼 상태를 업데이트합니다.
/// </summary>
/// <param name="currentTemporaryItemCount">현재 임시 배치된 아이템 스택의 수량</param>
public void NotifySlotStateChanged(int currentTemporaryItemCount)
{
    if (submitButton == null || questDropSlot == null) return;
    
    int needed = questDropSlot.RequiredAmount - questDropSlot.submittedCount;

    // 현재 임시 아이템 스택이 남은 요구량을 충족하는지 확인
    bool canSubmit = currentTemporaryItemCount >= needed;
    
    submitButton.interactable = canSubmit;
    
    Debug.Log($"[QuestManager] 제출 버튼 상태 업데이트: 필요 수량: {needed}, 임시 수량: {currentTemporaryItemCount}. 버튼 활성화: {canSubmit}");
}

}