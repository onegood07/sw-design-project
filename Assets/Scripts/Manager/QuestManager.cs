using UnityEngine;

/// <summary>
/// 모든 퀘스트의 진행 상태와 로직을 관리하는 싱글톤입니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // 싱글톤
    public static QuestManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    // 퀘스트 슬롯으로부터 아이템 제출 알림을 받는 함수
    public void OnItemSubmitted(string itemName, int amount, QuestSlot targetSlot)
    {
        // QuestSlot에서 이미 submittedCount를 업데이트 했으므로,
        // 여기서는 최종 상태를 확인하고 퀘스트 완료 처리를 합니다.
        
        Debug.Log($"[QuestManager] {itemName} {amount}개 제출 받음. 현재 진행도: {targetSlot.submittedCount}/{targetSlot.requiredAmount}");

        // 퀘스트 목표 달성 확인
        if (targetSlot.submittedCount >= targetSlot.requiredAmount)
        {
            Debug.Log($"🎉 퀘스트 목표 달성! [{targetSlot.requiredItemName}] 보상 지급 로직을 실행합니다.");
            HandleQuestCompletion(targetSlot);
        }
    }

    // 퀘스트 완료 시 보상 지급, 퀘스트 창 닫기 등을 처리
    private void HandleQuestCompletion(QuestSlot completedSlot)
    {
        // 1. 보상 지급 로직 (예: Inventory.instance.AddItem(RewardItem, 1);)
        // 2. NPC 대화 업데이트
        // 3. UI 변경 (예: '아이템 교환하기' 버튼 비활성화)
    }
}