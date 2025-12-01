using UnityEngine;

/// <summary>
/// 모든 퀘스트의 진행 상태와 로직을 관리하는 싱글톤입니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // 싱글톤
    public static QuestManager instance;

    // ⭐ 퀘스트 완료 보상 아이템: Item 타입으로 지정합니다. 
    // Unity Inspector에서 랜턴 아이템 데이터(Item 타입)를 연결해야 합니다.
    public Item lanternRewardItem; 

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
        // 1. 보상 지급 로직 (랜턴 1개 지급)
        if (Inventory.instance != null && lanternRewardItem != null)
        {
            // Inventory.instance.AddItem() 함수가 Item 타입을 받기 때문에 타입 오류 없이 작동합니다.
            Inventory.instance.AddItem(lanternRewardItem, 1); 
            Debug.Log($"[Reward] '랜턴' 아이템 1개를 인벤토리에 지급했습니다.");
        }
        else if (Inventory.instance == null)
        {
             Debug.LogError("[Reward Error] Inventory.instance가 null입니다. 인벤토리 시스템이 초기화되었는지 확인하세요.");
        }
        else if (lanternRewardItem == null)
        {
             Debug.LogError("[Reward Error] lanternRewardItem이 QuestManager 인스펙터에 연결되지 않았습니다.");
        }
        
        // 2. NPC 대화 업데이트 (필요하다면 구현)

        // 3. UI 변경 및 창 닫기 (자동 닫기 로직)
        // QuestUIController가 싱글톤이며 ClosePanel() 함수를 가지고 있다고 가정합니다.
        if (QuestUIController.instance != null)
        {
            QuestUIController.instance.ClosePanel();
            Debug.Log("[UI] 퀘스트 창을 자동으로 닫았습니다.");
        }
        else
        {
            Debug.LogWarning("[UI Error] QuestUIController.instance가 없어 창을 닫을 수 없습니다. QuestUIController를 싱글톤으로 설정하고 ClosePanel() 함수를 구현하세요.");
        }
    }
}