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
        HandleQuestCompletion(slot);
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

    // 💡 퀘스트가 완료되지 않은 상태에서 UI를 닫을 경우에만 DialogueManager를 정리합니다.
    if (activeQuestData != null) // 퀘스트 미완료 상태 (activeQuestData가 아직 null로 설정되지 않음)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue();
            Debug.Log("[CloseSubmitUI] 퀘스트 미완료 상태에서 UI 닫힘: DialogueManager.EndDialogue() 호출.");
        } else {
             Debug.LogError("[CloseSubmitUI Error] DialogueManager.Instance가 Null입니다!");
        }
    }

    // GameManager Null 체크 강화
    if (GameManager.Instance != null)
    {
        GameManager.Instance.EndInteraction();
        Debug.Log("[CloseSubmitUI] GameManager.EndInteraction() 호출 완료.");
    } else {
         Debug.LogWarning("[CloseSubmitUI] GameManager.Instance가 Null이므로 EndInteraction을 건너뜁니다.");
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
    
    // **가장 상위 Canvas를 끄는 것이 핵심입니다.**
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
    
    // ⭐⭐⭐ 핵심 수정: 버튼 리스너 명시적 제거 ⭐⭐⭐
    if (submitButton != null)
    {
        submitButton.onClick.RemoveAllListeners();
        Debug.Log("[CloseSubmitUI] SubmitButton 리스너를 모두 제거했습니다.");
    }
    // ⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐

    activeQuestData = null; // UI가 닫힐 때 완전히 초기화
    nextDialogueNodeIndex = -1;

    Debug.Log("[CloseSubmitUI] 상태 초기화 완료");
}

// ───────────────────────────────
// 퀘스트 완료 처리
private void HandleQuestCompletion(QuestSlot slot)
{
    Debug.Log($"[HandleQuestCompletion] 보상 지급 시작");

    // 1. 보상 지급 로직
    if (slot.RewardItem != null)
    {
        if (Inventory.instance != null)
        {
            Inventory.instance.AddItem(slot.RewardItem, slot.RewardCount);
            Debug.Log($"[HandleQuestCompletion] 보상 지급 완료: {slot.RewardItem.itemName} x{slot.RewardCount}");
        }
        else
        {
             Debug.LogError("[HandleQuestCompletion Error] Inventory.instance가 Null입니다! 보상 지급 실패.");
        }
    }
    else
    {
         Debug.Log("[HandleQuestCompletion] 지급할 보상 아이템이 없습니다.");
    }


    // 2. 대화 재개 (성공 시)
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
         Debug.LogWarning("[HandleQuestCompletion] 다음 노드 인덱스가 없습니다. 대화 재개 생략.");
    }

    activeQuestData = null; // 완료 후 activeQuestData를 null로 설정
    nextDialogueNodeIndex = -1;
    Debug.Log("[HandleQuestCompletion] 상태 초기화 완료");
}
}