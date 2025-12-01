using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image 사용을 위해 추가

public class QuestSlot : Slot
{
    // [설정] 요구 아이템 정보는 기존과 동일
    public string requiredItemName = "Pistol"; 
    public int requiredAmount = 2;            

    // [상태] 현재 이 슬롯에 제출된 아이템 개수 (확정 제출된 개수)
    [HideInInspector] public int submittedCount = 0;
    
    // ⭐ 임시 배치된 아이템 정보 저장용 변수
    // (드롭되었지만 아직 인벤토리에서 소비되지 않은 상태)
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1; // 임시로 온 인벤토리 슬롯 인덱스

    public override void OnDrop(PointerEventData eventData)
    {
        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;

        Slot fromSlot = drag.slot; // 드래그가 시작된 인벤토리 슬롯
        if (fromSlot == null || fromSlot == this) return;
        
        Inventory inven = Inventory.instance;
        if (inven == null) return;

        int fromIdx = fromSlot.slotIndex;
        InventoryItem draggedItem = inven.items.Count > fromIdx ? inven.items[fromIdx] : null;

        // 1. 아이템 유효성 검사 (요구 아이템인지 확인)
        if (draggedItem == null || draggedItem.itemName != requiredItemName)
        {
            Debug.LogWarning($"[Quest] 요구 아이템({requiredItemName})이 아닙니다.");
            return;
        }

        // 2. 임시 배치 정보 저장
        // 아이템의 개수나 데이터는 인벤토리 슬롯에 그대로 있지만, 
        // 퀘스트 슬롯에 해당 아이템 정보를 임시로 저장합니다.
        temporaryItem = draggedItem;
        temporaryItemIndex = fromIdx;
        
        // 3. UI 갱신 (임시로 놓인 아이템을 시각적으로 보여줌)
        // 🚨 중요: 여기서는 submittedCount가 아닌, draggedItem의 count를 표시하거나,
        // UI를 '아이템이 놓여짐' 상태로만 변경해야 합니다.
        UpdateTemporarySlotUI(draggedItem);
        
        Debug.Log($"아이템 {requiredItemName}이 슬롯에 임시 배치되었습니다.");
    }
    
    // ⭐ 임시 배치된 아이템을 표시하는 UI 함수
    private void UpdateTemporarySlotUI(InventoryItem itemData)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.itemImage;
            itemIcon.gameObject.SetActive(true);
        }
        // 임시 배치 상태에서는 개수를 표시하지 않거나, 전체 개수를 표시할 수 있습니다.
        if (itemCountText != null)
        {
            itemCountText.text = $"x {itemData.count}"; // 인벤토리 슬롯의 총 개수를 표시
            itemCountText.gameObject.SetActive(true);
        }
    }
    
    // ⭐ 퀘스트 슬롯의 상태를 초기화하는 함수 (예: 임시 배치된 아이템을 제거)
    public void ClearTemporarySlot()
    {
        temporaryItem = null;
        temporaryItemIndex = -1;
        
        if (itemIcon != null) itemIcon.gameObject.SetActive(false);
        if (itemCountText != null) itemCountText.gameObject.SetActive(false);
    }
    
    // QuestSlot.cs 스크립트에 다음 함수 추가

/// <summary>
/// 버튼이 눌렸을 때 임시 배치된 아이템을 확정 제출하는 함수
/// </summary>
public bool ConfirmSubmission()
{
    // 1. 임시 배치된 아이템이 있는지 확인
    if (temporaryItem == null || temporaryItemIndex == -1)
    {
        Debug.LogWarning("[Quest] 슬롯에 납입할 아이템이 없습니다.");
        return false;
    }
    
    Inventory inven = Inventory.instance;
    // 인벤토리에서 실제 아이템이 아직 존재하는지 재확인 (혹시 그 사이에 다른 곳에서 사용되었을 수 있음)
    InventoryItem actualItem = inven.items[temporaryItemIndex];
    
    if (actualItem == null || actualItem.itemName != requiredItemName)
    {
        Debug.LogError("[Quest] 인벤토리에서 아이템을 찾을 수 없거나 아이템이 변경되었습니다.");
        ClearTemporarySlot(); // 임시 배치 상태 초기화
        return false;
    }

    // 2. 제출할 개수 계산
    int needed = requiredAmount - submittedCount; // 더 필요한 개수
    if (needed <= 0)
    {
        Debug.Log($"[Quest] {requiredItemName}은 이미 충분히 제출되었습니다.");
        ClearTemporarySlot(); // 임시 배치 상태 초기화
        return true;
    }

    // 인벤토리에서 꺼낼 개수 (필요한 개수와 가진 개수 중 더 적은 쪽)
    int submitAmount = Mathf.Min(needed, actualItem.count);

    // 3. 인벤토리에서 아이템 소비 (제출)
    inven.ConsumeItemAt(temporaryItemIndex, submitAmount);

    // 4. 퀘스트 슬롯 상태 업데이트
    submittedCount += submitAmount;
    
    // 5. 퀘스트 매니저에게 제출 완료 알림
    QuestManager.instance.OnItemSubmitted(requiredItemName, submitAmount, this);
    
    // 6. UI 및 임시 상태 정리
    UpdateQuestSlotUI(temporaryItem); // 최종 제출 개수로 UI 갱신
    ClearTemporarySlot(); // 임시 배치 상태 초기화

    Debug.Log($"아이템 {requiredItemName} {submitAmount}개를 확정 납입했습니다. (총 {submittedCount}/{requiredAmount})");
    return true;
}
// QuestSlot.cs 파일 내의 클래스 정의 아무 곳에나 추가

// ⭐ 확정 제출 완료 후 최종 상태를 표시하는 UI 함수
private void UpdateQuestSlotUI(InventoryItem itemData)
{
    // 제출된 아이템의 아이콘을 표시
    if (itemIcon != null)
    {
        itemIcon.sprite = itemData.itemImage;
        itemIcon.gameObject.SetActive(true);
    }
    // 제출된 개수를 표시 (submittedCount를 사용)
    if (itemCountText != null)
    {
        // 확정 제출된 개수 / 요구 개수
        itemCountText.text = $"{submittedCount} / {requiredAmount}"; 
        itemCountText.gameObject.SetActive(true);
    }
}
}