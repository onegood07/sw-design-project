using UnityEngine;
using UnityEngine.UI; 

/// <summary>
/// 모든 퀘스트의 진행 상태와 로직을 관리하는 싱글톤입니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    // 퀘스트 제출 UI 필드 (Inspector 연결 필수)
    [Header("Quest Submission UI")]
    [Tooltip("퀘스트 제출 UI 전체 패널 (SubmitCanvas)")]
    public GameObject submitCanvas;     
    // 요구 아이템 이미지를 표시할 UI Image 컴포넌트를 연결하세요.
    public Image requiredItemImageComponent; 
    [Tooltip("요구 수량 표시 Text")]
    public Text submitAmountText;       
    public Button submitButton;         

    [Tooltip("아이템 납입을 받는 슬롯의 QuestSlot 컴포넌트를 연결하세요.")]
    public QuestSlot questDropSlot; 

    private QuestData activeQuestData; // 현재 NPC에게서 받은 활성 퀘스트 데이터
    
    // 퀘스트 완료 후 이어갈 Dialogue 노드 인덱스
    private int nextDialogueNodeIndex = -1; 
    
    // DialogueManager의 인스턴스를 직접 참조한다고 가정합니다.
    // private DialogueManager dialogueManager; 

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (submitCanvas != null) submitCanvas.SetActive(false);
    }
    
    /// <summary>
    /// NPC의 대화 시스템에서 호출됨: 제출 UI를 띄우고 다음 대화 노드 인덱스를 저장
    /// </summary>
    /// <param name="data">활성 퀘스트 데이터</param>
    /// <param name="nextIndex">납입 성공 후 DialogueManager가 이어서 진행할 노드의 인덱스</param>
    public void OpenSubmitUI(QuestData data, int nextIndex)
    {
        activeQuestData = data;
        nextDialogueNodeIndex = nextIndex; // 다음 노드 인덱스 저장

        if (submitCanvas == null)
        {
            Debug.LogError("[QuestManager] Submit Canvas가 Inspector에 연결되지 않았습니다.");
            return;
        }

        // 1. 요구 아이템 이미지 표시
        if (requiredItemImageComponent != null && data.requiredItemIcon != null)
        {
            requiredItemImageComponent.sprite = data.requiredItemIcon;
            requiredItemImageComponent.color = Color.white;
            requiredItemImageComponent.enabled = true;
        } else if (requiredItemImageComponent != null) {
            requiredItemImageComponent.enabled = false;
        }


        // 2. QuestSlot 초기화
        if (questDropSlot != null)
        {
            questDropSlot.SetupSlot(data); 
        }
        
        // 3. UI 데이터 업데이트 및 버튼 리스너 재설정
        if (questDropSlot != null && submitAmountText != null) 
            submitAmountText.text = $"수량: {questDropSlot.submittedCount} / {questDropSlot.RequiredAmount}";
        
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitButtonClicked); 
        }

        // 4. UI 띄우기
        submitCanvas.SetActive(true);
    }
    
    /// <summary>
    /// '납입/제출하기' 버튼 클릭 시 호출될 로직
    /// </summary>
    public void OnSubmitButtonClicked()
    {
        if (activeQuestData == null || questDropSlot == null) return;

        if (questDropSlot.ConfirmSubmission())
        {
            Debug.Log("[QuestManager] 아이템 제출이 성공적으로 처리되었습니다.");
        }
    }
    
    /// <summary>
    /// QuestSlot.ConfirmSubmission에서 아이템 제출이 발생할 때 호출됩니다.
    /// </summary>
    public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
    {
        if (slot.submittedCount >= slot.RequiredAmount)
        {
            Debug.Log($"[QuestManager] 퀘스트 '{activeQuestData.questName}' 완료! 보상 지급 및 대화 재개.");
            HandleQuestCompletion(slot);
            CloseSubmitUI();
        }
        else
        {
            // 수량 업데이트
            if (submitAmountText != null)
            {
                submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
            }
        }
    }
    
    public void CloseSubmitUI()
    {
        if (submitCanvas != null) submitCanvas.SetActive(false);
        if (requiredItemImageComponent != null) requiredItemImageComponent.enabled = false;
        if (questDropSlot != null) questDropSlot.ResetSlot();
    }
    
    /// <summary>
    /// 퀘스트 완료 시 보상 지급 및 정리
    /// </summary>
    private void HandleQuestCompletion(QuestSlot slot)
    {
        // 1. 보상 지급 로직...
        if (slot.RewardItem != null && Inventory.instance != null)
        {
            Inventory.instance.AddItem(slot.RewardItem, slot.RewardCount); 
        }
        
        // 2. 퀘스트 상태 완료로 설정 (필요하다면)
        // QuestManager.instance.SetQuestCompleted(slot.ActiveQuestData);

        // 3. ⭐⭐ 핵심: DialogueManager에게 다음 노드로 이어가도록 지시 (Instance로 수정) ⭐⭐
        if (DialogueManager.Instance != null && nextDialogueNodeIndex != -1)
        {
            // DialogueManager에 다음 노드를 진행하는 함수가 있다고 가정합니다.
            // 예를 들어, 현재 DialogueData와 저장된 노드 인덱스를 사용하여 대화를 재개합니다.
            DialogueManager.Instance.ContinueDialogueAtNode(nextDialogueNodeIndex);
        }
        
        // 모든 정리
        activeQuestData = null;
        nextDialogueNodeIndex = -1;
    }
}