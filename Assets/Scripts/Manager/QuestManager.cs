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
    
    // ⭐ 수정: 요구 아이템 이미지를 표시할 UI Image 컴포넌트를 연결
    [Tooltip("요구 아이템의 아이콘을 표시할 UI Image 컴포넌트를 연결하세요.")]
    public Image requiredItemImageComponent; 
    
    [Tooltip("요구 수량 표시 Text")]
    public Text submitAmountText;       
    public Button submitButton;         // '납입/제출하기' 버튼 

    // ⭐ QuestSlot 연결 (아이템 드롭을 처리하는 사용자님의 QuestSlot 스크립트)
    [Tooltip("아이템 납입을 받는 슬롯의 QuestSlot 컴포넌트를 연결하세요.")]
    public QuestSlot questDropSlot; 

    private QuestData activeQuestData; // 현재 NPC에게서 받은 활성 퀘스트 데이터

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject);
        
        // 시작 시 제출 UI 숨기기
        if (submitCanvas != null)
        {
            submitCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// DialogueManager에서 호출됨. 퀘스트 제출 UI를 띄우고 데이터 설정
    /// </summary>
    public void OpenSubmitUI(QuestData data)
    {
        activeQuestData = data;

        if (submitCanvas == null)
        {
            Debug.LogError("[QuestManager] Submit Canvas가 Inspector에 연결되지 않았습니다. 제출 UI를 띄울 수 없습니다.");
            return;
        }

        // 1. ⭐ 요구 아이템 이미지 표시 (QuestManager가 직접 처리)
        if (requiredItemImageComponent != null && data.requiredItemIcon != null)
        {
            requiredItemImageComponent.sprite = data.requiredItemIcon;
            requiredItemImageComponent.color = Color.white;
            requiredItemImageComponent.enabled = true;
        } else if (requiredItemImageComponent != null) {
            requiredItemImageComponent.enabled = false;
            Debug.LogWarning("[QuestManager] QuestData에 요구 아이콘이 없거나 Image 컴포넌트가 연결되지 않았습니다.");
        }


        // 2. QuestSlot 초기화 (납입 및 로직 처리)
        if (questDropSlot != null)
        {
            // QuestSlot의 SetupSlot 함수가 납입 관련 데이터만 받도록 설정합니다.
            // (QuestSlot의 SetupSlot 함수도 납입 UI 설정에 맞게 수정이 필요합니다.)
            questDropSlot.SetupSlot(data); 
        }
        else
        {
            Debug.LogError("[QuestManager] Quest Drop Slot이 Inspector에 연결되지 않았습니다. 아이템 납입이 불가능합니다.");
        }

        // 3. UI 데이터 업데이트 (초기 수량 표시)
        if (questDropSlot != null)
        {
            // 초기 수량 표시 (0 / 요구 수량)
            if (submitAmountText != null) 
                submitAmountText.text = $"수량: 0 / {questDropSlot.RequiredAmount}";
        }
        
        
        // 4. 제출 버튼 리스너 재설정
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitButtonClicked); 
        }

        // 5. UI 띄우기
        submitCanvas.SetActive(true);
    }
    
    /// <summary>
    /// '납입/제출하기' 버튼 클릭 시 호출될 로직
    /// </summary>
    public void OnSubmitButtonClicked()
    {
        if (activeQuestData == null || questDropSlot == null)
        {
            Debug.LogWarning("[QuestManager] 활성 퀘스트 데이터 또는 QuestSlot이 없습니다.");
            return;
        }

        // QuestSlot에게 확정 제출을 시도하도록 명령
        if (questDropSlot.ConfirmSubmission())
        {
            // 성공적으로 납입됨. OnItemSubmitted에서 완료 체크가 이루어짐.
        }
    }
    
    /// <summary>
    /// QuestSlot.ConfirmSubmission에서 아이템 제출이 발생할 때 호출됩니다.
    /// </summary>
    public void OnItemSubmitted(string itemName, int amountSubmitted, QuestSlot slot)
    {
        // 총 제출 수량 확인
        if (slot.submittedCount >= slot.RequiredAmount)
        {
            Debug.Log($"[QuestManager] 퀘스트 '{activeQuestData.questName}' 완료! 보상 지급.");
            HandleQuestCompletion(slot);
            CloseSubmitUI();
        }
        else
        {
            Debug.Log($"[QuestManager] {slot.submittedCount} / {slot.RequiredAmount} 제출됨. 계속 제출하세요.");
            // 제출 텍스트를 업데이트하여 남은 수량을 표시
            if (submitAmountText != null)
            {
                submitAmountText.text = $"수량: {slot.submittedCount} / {slot.RequiredAmount}";
            }
        }
    }
    
    public void CloseSubmitUI()
    {
        if (submitCanvas != null)
        {
            submitCanvas.SetActive(false);
        }
        
        // 이미지 초기화
        if (requiredItemImageComponent != null)
        {
            requiredItemImageComponent.enabled = false;
            requiredItemImageComponent.sprite = null;
        }
        
        // QuestSlot 리셋
        if (questDropSlot != null)
        {
            questDropSlot.ResetSlot();
        }

        activeQuestData = null;
    }
    
    /// <summary>
    /// 퀘스트 완료 시 보상 지급 및 정리
    /// </summary>
    private void HandleQuestCompletion(QuestSlot slot)
    {
        string rewardName = slot.RewardItem != null ? slot.RewardItem.name : "미정";
        
        if (slot.RewardItem != null)
        {
            // Inventory.instance.AddItem()을 사용하여 보상 지급
            if (Inventory.instance != null)
            {
                // Inventory.instance.AddItem()이 Item 타입을 받도록 가정합니다.
                Inventory.instance.AddItem(slot.RewardItem, slot.RewardCount); 
                Debug.Log($"[Reward SUCCESS] '{rewardName}' {slot.RewardCount}개를 인벤토리에 추가했습니다. (Inventory.instance 사용)");
            }
            else
            {
                Debug.LogError("[Reward ERROR] Inventory.instance를 찾을 수 없습니다. 보상 지급 실패.");
            }
        } else {
            Debug.LogWarning("[Reward] 지급할 보상 아이템이 QuestData에 설정되지 않았습니다.");
        }
    }
}