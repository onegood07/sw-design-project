using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

public class QuestSlot : Slot
{
    // ⭐ [추가]: QuestManager가 보상 타입(아이템/생존자)을 확인하고 처리할 수 있도록 QuestData 객체 자체를 저장합니다.
    [HideInInspector] public QuestData AssignedQuestData; 

    // QuestManager가 읽을 수 있도록 public get 속성
    public int RequiredAmount => requiredAmount;
    public string RequiredItemName => requiredItemName;
    public Item RewardItem => rewardItem; 
    public int RewardCount => rewardCount;
    
    // 퀘스트 요구/보상 정보를 저장할 내부 private 변수
    public string requiredItemName;
    public int requiredAmount;
    public Item rewardItem; 
    public int rewardCount; 

    [Header("Quest Status")]
    [HideInInspector] public int submittedCount = 0;
    
    // 임시 배치된 아이템 정보 (납입 버튼 누르기 전 상태)
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1; 
    
    // QuestSlot의 Raycast Target을 담당하는 Image 컴포넌트 (슬롯 배경)
    private Image slotBackground;

    void Awake()
    {
        // 슬롯 자체의 Image 컴포넌트를 가져옴
        slotBackground = GetComponent<Image>();
        
        if (slotBackground != null)
        {
            slotBackground.raycastTarget = true;
        }
        else
        {
             Debug.LogWarning("[QuestSlot] QuestSlot에 Image 컴포넌트를 찾을 수 없습니다. 드롭 이벤트가 작동하지 않을 수 있습니다.");
        }
    }
    
    /// <summary>
    /// QuestData 객체를 주입받아 슬롯을 초기화하는 함수.
    /// QuestManager의 OpenSubmitUI에서 호출됩니다.
    /// </summary>
    /// <param name="data">초기화에 필요한 QuestData ScriptableObject</param>
    public void SetupSlot(QuestData data) 
    {
        if (data == null)
        {
            Debug.LogError("[QuestSlot] QuestData가 null입니다. 초기화 실패.");
            return;
        }

        AssignedQuestData = data; 
        this.requiredItemName = data.requiredItemName;
        this.requiredAmount = data.requiredAmount;
        this.rewardItem = data.rewardItem;
        this.rewardCount = data.rewardCount;
        this.submittedCount = 0; 
        
        ClearTemporarySlot();
        
        if (itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {this.requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
        
        Debug.Log($"[QuestSlot] 슬롯 초기화 완료: 요구: {this.requiredItemName} x {this.requiredAmount}, 보상: {(this.rewardItem != null ? this.rewardItem.name : "없음")} x {this.rewardCount}");
    }

    // 아이템 드롭 처리: 요구 아이템인지, 수량이 충분한지 확인
   public override void OnDrop(PointerEventData eventData) {
    {
        if (string.IsNullOrEmpty(requiredItemName) || requiredAmount <= 0)
        {
            Debug.LogWarning("[QuestSlot] 이 슬롯은 아직 퀘스트 정보가 설정되지 않았습니다.");
            return;
        }
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
        
        // 임시 배치 상태 업데이트
        temporaryItem = draggedItem;
        temporaryItemIndex = fromIdx;
        UpdateTemporarySlotUI(draggedItem);
        Debug.Log($"아이템 {requiredItemName}이 슬롯에 임시 배치되었습니다.");
        
        // ⭐ [수정] 아이템을 드롭했을 때 QuestManager에게 상태 변경을 알립니다.
        if (QuestManager.instance != null)
        {
            // 드롭된 아이템의 수량으로 버튼 상태 업데이트를 요청
            QuestManager.instance.NotifySlotStateChanged(draggedItem.count);
        }
    }
   }
    
    /// <summary>
    /// 버튼이 눌렸을 때 임시 배치된 아이템을 확정 제출하는 함수 (QuestManager에서 호출됨)
    /// </summary>
    public bool ConfirmSubmission()
    {
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
        
        // 인벤토리에서 실제 아이템 확인
        InventoryItem actualItem = inven.items[temporaryItemIndex];
        if (actualItem == null || actualItem.itemName != requiredItemName)
        {
            Debug.LogError("[Quest] 인벤토리에서 아이템을 찾을 수 없거나 아이템이 변경되었습니다.");
            ClearTemporarySlot(); 
            return false;
        }
        
        int needed = requiredAmount - submittedCount; 
        if (needed <= 0)
        {
            Debug.Log($"[Quest] {requiredItemName}은 이미 충분히 제출되었습니다.");
            ClearTemporarySlot(); 
            return true; // 이미 완료된 것으로 간주
        }
        
        int potentialSubmitAmount = actualItem.count;
        
        // ⭐⭐⭐ 핵심 수정: 버튼 비활성화 정책으로 인해 이 블록은 거의 실행되지 않아야 하지만, 안전 장치로 남깁니다. ⭐⭐⭐
        if (potentialSubmitAmount < needed)
        {
           // 🚨 제출 거부: 버튼이 비활성화되었어야 합니다. 코드가 여기에 도달하면 버그입니다.
            Debug.LogError($"[Quest] FATAL ERROR: 수량 부족({potentialSubmitAmount}/{needed})으로 제출 거부됨. 버튼 비활성화 로직 확인 필요.");
            
            // UI를 띄우지 않고 롤백 (임시 아이템 해제)
            ClearTemporarySlot(); 
            return false;
        }
        
        // 요구량을 충족할 수 있는 경우에만 소모 로직 진행
        int submitAmount = needed; // 요구량 전체만 소모

        // 인벤토리에서 아이템 소모
        inven.ConsumeItemAt(temporaryItemIndex, submitAmount);
        submittedCount += submitAmount;
        
        // UI 업데이트
        UpdateQuestSlotUI(temporaryItem); 
        
        // QuestManager에게 제출 완료 알림 (보상 지급 트리거)
        if (QuestManager.instance != null)
        {
            QuestManager.instance.OnItemSubmitted(requiredItemName, submitAmount, this); 
        }
        
        ClearTemporarySlot(); // 임시 배치 상태 해제 -> NotifySlotStateChanged(0) 호출
        
        Debug.Log($"아이템 {requiredItemName} {submitAmount}개를 확정 납입했습니다. (총 {submittedCount}/{requiredAmount})");
        
        return true; // 제출 성공
    }

    // 임시 배치된 아이템의 정보(개수)를 보여주는 UI 업데이트
    private void UpdateTemporarySlotUI(InventoryItem itemData)
    {
        if (itemIcon == null) 
        {
            Debug.LogError("[UI NRE Check] UpdateTemporarySlotUI: itemIcon이 Slot 인스펙터에 연결되지 않았습니다. 임시 UI 업데이트 실패."); 
            return;
        }
        itemIcon.sprite = itemData.itemImage;
        itemIcon.gameObject.SetActive(true);
        if (itemCountText != null)
        {
            itemCountText.text = $"x {itemData.count}"; // 임시로 드래그된 아이템의 전체 수량 표시
            itemCountText.gameObject.SetActive(true);
        }
    }
    
    // 최종 제출 후 현재 진행 상태를 보여주는 UI 업데이트
    private void UpdateQuestSlotUI(InventoryItem itemData)
    {
        if (itemIcon == null) 
        {
            Debug.LogError("[UI NRE Check] UpdateQuestSlotUI: itemIcon이 Slot 인스펙터에 연결되지 않았습니다. 확정 UI 업데이트 건너뜀."); 
            return;
        }
        itemIcon.sprite = itemData.itemImage; 
        itemIcon.gameObject.SetActive(true);
        if (itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {requiredAmount}"; // 제출된 수량 표시
            itemCountText.gameObject.SetActive(true);
        }
    }

    
    // 임시 배치 상태를 해제하고, 제출된 아이템 수량이 0일 경우 UI를 숨김
    public void ClearTemporarySlot()
    {
        temporaryItem = null;
        temporaryItemIndex = -1;
        
        // 임시 배치 상태만 해제하고, 제출된 카운트는 유지합니다.
        
        // 만약 제출된 아이템이 없다면 아이콘과 텍스트를 숨깁니다.
        if (submittedCount == 0)
        {
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (itemCountText != null) itemCountText.gameObject.SetActive(false);
        } 
        // 제출된 아이템이 있다면, 현재 상태(submittedCount)를 다시 표시합니다.
        else if (itemIcon != null && itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {requiredAmount}";
        }
        
        // ⭐ [추가]: 임시 아이템이 제거되었음을 QuestManager에게 알립니다 (수량: 0).
        // 이로 인해 Submit 버튼이 비활성화됩니다.
        if (QuestManager.instance != null)
        {
            QuestManager.instance.NotifySlotStateChanged(0);
        }
    }
    
    /// <summary>
    /// 퀘스트 제출 UI가 닫히거나, 제출이 확정되지 않았을 때 임시 배치 상태를 해제합니다.
    /// QuestManager의 CloseSubmitUI에서 호출됩니다.
    /// </summary>
    public void RollbackSubmission()
    {
        if (temporaryItem != null)
        {
             Debug.Log($"[QuestSlot] 퀘스트 취소로 인해 임시 배치된 아이템 ({temporaryItem.itemName})의 상태를 해제합니다.");
        }
        
        // 임시 배치 정보 초기화 및 UI 정리 (NotifySlotStateChanged(0) 포함)
        ClearTemporarySlot(); 
    }
    
    /// <summary>
    /// 퀘스트 완료 후 슬롯 상태를 완전히 초기화합니다. (QuestManager에서 호출)
    /// </summary>
    public void ResetSlot()
    {
        // 모든 퀘스트 정보를 초기화합니다.
        this.AssignedQuestData = null; 
        this.requiredItemName = null;
        this.requiredAmount = 0;
        this.rewardItem = null;
        this.rewardCount = 0;
        this.submittedCount = 0;
        
        // UI도 완전히 숨깁니다. (NotifySlotStateChanged(0) 포함)
        ClearTemporarySlot(); 
    }
}