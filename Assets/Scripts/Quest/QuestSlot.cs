using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

// Slot 클래스를 상속받는다고 가정하며, itemIcon과 itemCountText는 Slot에 정의되어 있다고 가정합니다.
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

    // 가정: itemIcon과 itemCountText는 부모 클래스 Slot에 정의되어 있습니다.

    void Awake()
    {
        // 슬롯 자체의 Image 컴포넌트를 가져옴
        slotBackground = GetComponent<Image>();
        
        // ⭐ [추가된 로직] 슬롯이 항상 드롭 가능하도록 Raycast Target을 강제로 활성화
        if (slotBackground != null)
        {
            // 이 설정을 통해 itemIcon의 SetActive 상태와 무관하게 슬롯 영역 클릭 가능
            slotBackground.raycastTarget = true;
            // Debug.Log("[QuestSlot] 배경 Raycast Target 활성화됨."); 
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

        // ⭐ [수정]: QuestData 객체 자체를 저장합니다.
        AssignedQuestData = data; 

        // 1. 요구 사항 데이터 저장
        this.requiredItemName = data.requiredItemName;
        this.requiredAmount = data.requiredAmount;
        
        // 2. 보상 데이터 저장 (물물교환/보상에 사용됨)
        this.rewardItem = data.rewardItem;
        this.rewardCount = data.rewardCount;
        
        this.submittedCount = 0; // 퀘스트 시작 시 초기화
        
        // 초기 퀘스트 상태 UI 업데이트 (예: 0 / reqAmount 표시)
        ClearTemporarySlot();
        
        // Slot 클래스에 itemCountText가 있다고 가정
        if (itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {this.requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
        
        Debug.Log($"[QuestSlot] 슬롯 초기화 완료: 요구: {this.requiredItemName} x {this.requiredAmount}, 보상: {(this.rewardItem != null ? this.rewardItem.name : "없음")} x {this.rewardCount}");
    }

    public void RollbackSubmission()
{
    // 현재 QuestSlot의 OnDrop 로직은 아이템을 인벤토리에서 '제거'하지 않고 
    // 'temporaryItem'으로 정보만 저장합니다.
    
    // 따라서 롤백은 단순히 임시 배치 상태를 클리어하고, 
    // 제출 완료되지 않은 상태의 UI를 다시 표시하는 것으로 충분합니다.
    
    if (temporaryItem != null)
    {
         Debug.Log($"[QuestSlot] 퀘스트 취소로 인해 임시 배치된 아이템 ({temporaryItem.itemName})의 상태를 해제합니다.");
    }
    
    // 임시 배치 정보 초기화 및 UI 정리
    ClearTemporarySlot(); 
    
    // 이 시점에서 인벤토리에서 아이템이 소모되지 않았으므로 별도로 돌려줄 필요가 없습니다.
}

    // 아이템 드롭 처리: 요구 아이템인지, 수량이 충분한지 확인
    public override void OnDrop(PointerEventData eventData)
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
        
        // Inventory.instance, ItemDragHandler.currentlyDragging 등은 외부 스크립트/싱글톤에서 가져온다고 가정
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
    }
    
// QuestSlot.cs 스크립트
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
    
    // 인벤토리에서 실제 아이템 확인 (드래그 후 인벤토리 변경이 발생했을 수 있으므로 재확인)
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
    
    // 제출하고자 하는 수량 (현재 인벤토리 슬롯에 있는 수량)
    int potentialSubmitAmount = actualItem.count;
    
    // ⭐⭐⭐ 핵심 수정: 제출 버튼을 누르면, 현재 인벤토리 슬롯의 아이템 전체를 제출한다고 가정합니다. ⭐⭐⭐
    // 하지만 요구량(needed)보다 적은 수량을 제출하면 아이템이 소모되고 퀘스트는 미완료 상태로 남아 유실감을 줍니다.
    
    // 만약 현재 슬롯에 있는 아이템을 다 털어도 요구량(needed)에 미치지 못한다면, 제출을 거부하고 롤백합니다.
    if (potentialSubmitAmount < needed)
    {
        // 🚨 제출 거부: 요구량을 충족하지 못했으므로 소모하지 않습니다.
        // 현재 로직은 드롭 시 소모하지 않았으므로, 임시 배치 상태만 해제합니다.
        Debug.LogWarning($"[Quest] {requiredItemName} (현재 {potentialSubmitAmount}개)는 요구량({needed}개)을 충족하지 못합니다. 제출을 거부합니다.");
        ClearTemporarySlot(); // 임시 배치 상태 해제
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
    
    ClearTemporarySlot(); // 임시 배치 상태 해제
    
    Debug.Log($"아이템 {requiredItemName} {submitAmount}개를 확정 납입했습니다. (총 {submittedCount}/{requiredAmount})");
    
    return true; // 제출 성공
}
    
    
    
    
    
    // 임시 배치된 아이템의 정보(개수)를 보여주는 UI 업데이트
    private void UpdateTemporarySlotUI(InventoryItem itemData)
    {
        // Slot 클래스에 itemIcon이 있다고 가정
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
        // Slot 클래스에 itemIcon이 있다고 가정
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
            // 아이콘은 이미 설정되어 있다고 가정하고 수량만 업데이트
            itemCountText.text = $"{submittedCount} / {requiredAmount}";
        }
    }
    
    /// <summary>
    /// 퀘스트 완료 후 슬롯 상태를 완전히 초기화합니다. (QuestManager에서 호출)
    /// </summary>
    public void ResetSlot()
    {
        // 모든 퀘스트 정보를 초기화합니다.
        this.AssignedQuestData = null; // ⭐ [추가]: QuestData도 초기화
        this.requiredItemName = null;
        this.requiredAmount = 0;
        this.rewardItem = null;
        this.rewardCount = 0;
        this.submittedCount = 0;
        
        // UI도 완전히 숨깁니다.
        ClearTemporarySlot(); 
    }
}