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
    [Tooltip("요구 아이템 이름 표시 Text")]
    public Text submitItemText;         
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

        // ⭐ 1. QuestSlot 초기화 (사용자님의 SetupSlot 호출)
        if (questDropSlot != null)
        {
            // 이 함수를 호출하여 QuestSlot에 퀘스트 정보를 주입합니다.
            questDropSlot.SetupSlot(data); 
        }
        else
        {
            Debug.LogError("[QuestManager] Quest Drop Slot이 Inspector에 연결되지 않았습니다. 아이템 납입이 불가능합니다.");
        }

        // 2. UI 데이터 업데이트 (QuestSlot의 정보 사용)
        if (questDropSlot != null)
        {
            if (submitItemText != null) 
                submitItemText.text = "요구 품목: " + questDropSlot.RequiredItemName;
            
            if (submitAmountText != null) 
                submitAmountText.text = "수량: " + questDropSlot.RequiredAmount;
        }
        
        
        // 3. 제출 버튼 리스너 재설정
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            // 버튼 클릭 시 QuestSlot이 가진 ConfirmSubmission 로직을 호출하도록 연결합니다.
            submitButton.onClick.AddListener(OnSubmitButtonClicked); 
        }

        // 4. UI 띄우기
        submitCanvas.SetActive(true);
        Debug.Log("[QuestManager] Submit Canvas를 활성화했습니다.");
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

        // ⭐ 핵심: QuestSlot에게 확정 제출을 시도하도록 명령
        // ConfirmSubmission 내부에서 인벤토리 확인/아이템 소모/QuestManager.OnItemSubmitted 호출이 이루어짐
        if (questDropSlot.ConfirmSubmission())
        {
            Debug.Log("[QuestManager] 아이템 제출이 성공적으로 처리되었습니다. 완료 여부 확인 중...");
        }
        else
        {
            Debug.Log("제출 실패: 아이템이 없거나 조건이 맞지 않습니다. (QuestSlot에서 처리됨)");
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
            // 퀘스트 완료 로직 (보상 지급)
            HandleQuestCompletion(slot);
            CloseSubmitUI();
        }
        else
        {
            Debug.Log($"[QuestManager] {slot.submittedCount} / {slot.RequiredAmount} 제출됨. 계속 제출하세요.");
            // 제출 텍스트를 업데이트하여 남은 수량을 표시 (선택 사항)
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
        
        // QuestSlot 리셋
        if (questDropSlot != null)
        {
            questDropSlot.ClearTemporarySlot();
            // ClearTemporarySlot은 submittedCount가 0이 아니면 UI를 유지하므로, 
            // 퀘스트가 완료될 때만 리셋해야 합니다. (HandleQuestCompletion에서 처리)
        }

        activeQuestData = null;
    }
    
    /// <summary>
    /// 퀘스트 완료 시 보상 지급 및 정리
    /// </summary>
    private void HandleQuestCompletion(QuestSlot slot)
    {
        // 퀘스트 데이터에서 보상 정보를 가져옵니다.
        // slot.RewardItem은 Item (MonoBehaviour) 타입입니다.
        string rewardName = slot.RewardItem != null ? slot.RewardItem.name : "미정";
        
        Debug.Log($"[Reward] '{rewardName}' {slot.RewardCount}개 지급.");
        
        // ⭐ [핵심 수정 부분] Item 타입에서 ItemData 타입인 itemDataAsset 필드를 추출하여 전달
        if (slot.RewardItem != null)
        {
            // 인벤토리 매니저의 싱글톤 인스턴스가 존재하고 AddItem 함수를 가지고 있다고 가정합니다.
            if (InventoryManager.Instance != null)
            {
                // [CS1503 해결] slot.RewardItem (Item)이 아닌, 
                // 그 안의 slot.RewardItem.itemDataAsset (ItemData)를 넘깁니다.
                InventoryManager.Instance.AddItem(slot.RewardItem.itemDataAsset, slot.RewardCount); 
                Debug.Log($"[Reward SUCCESS] '{rewardName}' {slot.RewardCount}개를 인벤토리에 추가했습니다.");
            }
            else
            {
                // 인벤토리 매니저가 없으면 아이템을 추가할 수 없습니다.
                Debug.LogError("[Reward ERROR] InventoryManager.instance를 찾을 수 없습니다. 보상 지급 실패.");
            }
        } else {
            Debug.LogWarning("[Reward] 지급할 보상 아이템이 QuestData에 설정되지 않았습니다.");
        }
        
        // 퀘스트 슬롯 완전 리셋 (다음 퀘스트를 위해)
        slot.ResetSlot();
        
        // (추가 퀘스트 추적 로직: 퀘스트 완료 목록에 추가 등)
    }
}