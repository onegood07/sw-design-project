using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

// Slot 클래스를 상속받는다고 가정하며, itemIcon과 itemCountText는 Slot에 정의되어 있다고 가정합니다.
public class QuestSlot : Slot
{
    // ⭐ itemIcon 및 itemCountText는 상위 클래스 Slot에서 상속받아 사용합니다.
    
    [Header("Quest Requirements")]
    public string requiredItemName = "Pistol"; 
    public int requiredAmount = 2;            

    [Header("Quest Status")]
    [HideInInspector] public int submittedCount = 0;
    
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1; 

    public override void OnDrop(PointerEventData eventData)
    {
        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;

        Slot fromSlot = drag.slot; 
        if (fromSlot == null || fromSlot == this) return;
        
        Inventory inven = Inventory.instance;
        if (inven == null) 
        {
            Debug.LogError("[QuestSlot] Inventory.instance가 null입니다.");
            return;
        }

        int fromIdx = fromSlot.slotIndex;
        InventoryItem draggedItem = inven.items.Count > fromIdx ? inven.items[fromIdx] : null;

        if (draggedItem == null || draggedItem.itemName != requiredItemName)
        {
            Debug.LogWarning($"[Quest] 요구 아이템({requiredItemName})이 아닙니다.");
            return;
        }
        
        if (submittedCount >= requiredAmount)
        {
            Debug.Log($"[Quest] {requiredItemName}은 이미 충분히 제출되었습니다. 임시 배치를 허용하지 않습니다.");
            return;
        }

        temporaryItem = draggedItem;
        temporaryItemIndex = fromIdx;
        
        UpdateTemporarySlotUI(draggedItem);
        
        Debug.Log($"아이템 {requiredItemName}이 슬롯에 임시 배치되었습니다.");
    }
    
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
        if (inven == null) 
        {
            Debug.LogError("[QuestSlot] Inventory.instance가 null입니다.");
            ClearTemporarySlot();
            return false;
        }

        InventoryItem actualItem = inven.items[temporaryItemIndex];
        
        if (actualItem == null || actualItem.itemName != requiredItemName)
        {
            Debug.LogError("[Quest] 인벤토리에서 아이템을 찾을 수 없거나 아이템이 변경되었습니다.");
            ClearTemporarySlot(); 
            return false;
        }

        // 2. 제출할 개수 계산
        int needed = requiredAmount - submittedCount; 
        if (needed <= 0)
        {
            Debug.Log($"[Quest] {requiredItemName}은 이미 충분히 제출되었습니다.");
            ClearTemporarySlot(); 
            return true;
        }

        int submitAmount = Mathf.Min(needed, actualItem.count);

        // 3. 인벤토리에서 아이템 소비 (제출)
        inven.ConsumeItemAt(temporaryItemIndex, submitAmount);

        // 4. 퀘스트 슬롯 상태 업데이트
        submittedCount += submitAmount;
        
        // ⭐⭐⭐ 5. UI 업데이트를 퀘스트 매니저 알림보다 먼저 수행합니다! ⭐⭐⭐
        // 이렇게 하면 UI가 닫히기 전에 최종 상태를 표시할 수 있습니다.
        UpdateQuestSlotUI(temporaryItem); 
        
        // 6. 퀘스트 매니저에게 제출 완료 알림 (여기서 QuestManager는 패널 닫기 로직을 호출합니다.)
        QuestManager.instance.OnItemSubmitted(requiredItemName, submitAmount, this);
        
        // 7. 임시 상태 정리
        ClearTemporarySlot(); 

        Debug.Log($"아이템 {requiredItemName} {submitAmount}개를 확정 납입했습니다. (총 {submittedCount}/{requiredAmount})");
        return true;
    }


    // ⭐ 임시 배치된 아이템을 표시하는 UI 함수
    private void UpdateTemporarySlotUI(InventoryItem itemData)
    {
        // NRE 방지를 위해 itemIcon이 Slot 클래스에서 연결되었는지 확인
        if (itemIcon == null) 
        {
            Debug.LogError("[UI NRE Check] UpdateTemporarySlotUI: itemIcon이 Slot 인스펙터에 연결되지 않았습니다. 임시 UI 업데이트 실패."); 
            return;
        }
        itemIcon.sprite = itemData.itemImage;
        itemIcon.gameObject.SetActive(true);
        
        if (itemCountText != null)
        {
            itemCountText.text = $"x {itemData.count}"; 
            itemCountText.gameObject.SetActive(true);
        }
    }
    
    // ⭐ 확정 제출 완료 후 최종 상태를 표시하는 UI 함수
    private void UpdateQuestSlotUI(InventoryItem itemData)
    {
        // NRE 방지 로직 강화: UI 컴포넌트 널 체크를 더 철저히 하여 널 참조 오류를 방지
        if (itemIcon == null) 
        {
            Debug.LogError("[UI NRE Check] UpdateQuestSlotUI: itemIcon이 Slot 인스펙터에 연결되지 않았습니다. 확정 UI 업데이트 건너뜀."); 
            return;
        }

        // 아이콘 업데이트 (이 코드가 이제 먼저 실행됩니다.)
        itemIcon.sprite = itemData.itemImage; 
        itemIcon.gameObject.SetActive(true);

        // 개수 텍스트 업데이트
        if (itemCountText != null)
        {
            // 확정 제출된 개수 / 요구 개수
            itemCountText.text = $"{submittedCount} / {requiredAmount}"; 
            itemCountText.gameObject.SetActive(true);
        }
    }

    
    // ⭐ 퀘스트 슬롯의 상태를 초기화하는 함수 (예: 임시 배치된 아이템을 제거)
    public void ClearTemporarySlot()
    {
        temporaryItem = null;
        temporaryItemIndex = -1;
        
        // 제출된 아이템이 0개인 경우에만 UI를 숨깁니다.
        if (submittedCount == 0)
        {
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (itemCountText != null) itemCountText.gameObject.SetActive(false);
        }
    }
}