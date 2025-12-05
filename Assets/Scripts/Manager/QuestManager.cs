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
    // OpenSubmitUI: UI 열기 (✅ 인벤토리 열기 로직 추가)
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
            // submittedCount와 RequiredAmount를 QuestSlot에서 가져온다고 가정
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

        // 1. SubmitCanvas 활성화
        if (submitCanvas != null)
            submitCanvas.SetActive(true);

        // 2. QuestPanel 활성화
        if (QuestPanel != null)
            QuestPanel.SetActive(true);
        
        // 3. SubmitImagePanel 활성화 (✅ 핵심: 콘텐츠가 보이도록 함)
        if (submitImagePanel != null)
            submitImagePanel.SetActive(true);
        else
            Debug.LogError("[QuestManager] SubmitImagePanel이 Inspector에 연결되지 않았습니다. UI 콘텐츠가 보이지 않습니다.");

        // 4. ✅ 인벤토리 UI 활성화 (퀘스트 납입 시 인벤토리 열기)
        if (InventoryUI.instance != null) 
        {
            InventoryUI.instance.OpenInventory();
            Debug.Log("[QuestManager] 퀘스트 제출 UI와 함께 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[QuestManager] InventoryUI 인스턴스를 찾을 수 없습니다. 인벤토리를 열 수 없습니다.");
        }

        Debug.Log("[OpenSubmitUI] UI 열림 완료");
    }

    // ───────────────────────────────
    // 제출 버튼
    public void OnSubmitButtonClicked()
    {
        if (activeQuestData == null || questDropSlot == null) return;

        // ConfirmSubmission이 성공적으로 아이템 소모 및 카운트 증가 처리 후 true를 반환한다고 가정
        if (questDropSlot.ConfirmSubmission())
        {
            Debug.Log("[OnSubmitButtonClicked] 아이템 제출 성공");
            // 아이템 제출 후 OnItemSubmitted 로직이 실행되어 퀘스트 완료 여부를 판단해야 함
            // OnItemSubmitted(activeQuestData.requiredItemName, submittedAmount, questDropSlot);
        }
    }
    
    // ───────────────────────────────
    // 아이템 제출 시 (QuestSlot 또는 Inventory 시스템에서 호출된다고 가정)
    public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
    {
        if (slot.submittedCount >= slot.RequiredAmount)
        {
            Debug.Log($"[OnItemSubmitted] 퀘스트 '{activeQuestData.questName}' 완료!");
            HandleQuestCompletion(slot);
            CloseSubmitUI(); // 완료 후 자동으로 UI 닫기
        }
        else
        {
            if (submitAmountText != null)
                submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
        }
    }

    // ───────────────────────────────
    /// <summary>
    /// UI 닫기 (X 버튼 클릭 또는 완료 후 호출) (✅ 인벤토리 닫기 로직 추가)
    /// </summary>
    public void CloseSubmitUI()
    {
        Debug.Log("[CloseSubmitUI] 호출됨. 제출 UI와 대화 상태 정리 시작.");

        // DialogueManager 상태 강제 종료 (대화 재개 문제 해결 로직)
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            // DialogueManager의 EndDialogue()를 호출하여 currentNPC, IsDialogueActive 등을 정리
            DialogueManager.Instance.EndDialogue(); 
            Debug.Log("[CloseSubmitUI] DialogueManager 상태 강제 종료 처리 완료.");
        }

        // 4. ✅ 인벤토리 UI 비활성화
        if (InventoryUI.instance != null) 
        {
            InventoryUI.instance.CloseInventory();
            Debug.Log("[CloseSubmitUI] 인벤토리 UI를 닫았습니다.");
        }

        // 1. SubmitImagePanel 비활성화
        if (submitImagePanel != null)
            submitImagePanel.SetActive(false);
        
        // 2. QuestPanel 비활성화
        if (QuestPanel != null)
            QuestPanel.SetActive(false);
        
        // 3. SubmitCanvas 비활성화
        if (submitCanvas != null) 
            submitCanvas.SetActive(false); 
            
        if (requiredItemImageComponent != null) requiredItemImageComponent.enabled = false;
        if (questDropSlot != null)
        {
            questDropSlot.ResetSlot();
            questDropSlot.gameObject.SetActive(false); // 슬롯 오브젝트도 명시적으로 끔
        }

        activeQuestData = null;
        nextDialogueNodeIndex = -1;

        Debug.Log("[CloseSubmitUI] 상태 초기화 완료");
    }

    // ───────────────────────────────
    // 퀘스트 완료 처리
    private void HandleQuestCompletion(QuestSlot slot)
    {
        Debug.Log($"[HandleQuestCompletion] 보상 지급 시작");

        // 보상 지급 로직 (Inventory 클래스가 존재한다고 가정)
        if (slot.RewardItem != null && Inventory.instance != null)
        {
            Inventory.instance.AddItem(slot.RewardItem, slot.RewardCount);
            Debug.Log($"[HandleQuestCompletion] 보상: {slot.RewardItem.itemName} x{slot.RewardCount}");
        }

        // 대화 재개 (성공 시)
        if (DialogueManager.Instance != null && nextDialogueNodeIndex != -1)
        {
            DialogueManager.Instance.ContinueDialogueAtNode(nextDialogueNodeIndex);
            Debug.Log($"[HandleQuestCompletion] 다음 노드 진행: {nextDialogueNodeIndex}");
        }

        activeQuestData = null;
        nextDialogueNodeIndex = -1;
        Debug.Log("[HandleQuestCompletion] 상태 초기화 완료");
    }
}