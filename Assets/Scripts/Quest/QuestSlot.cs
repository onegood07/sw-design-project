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
        
        // 필요한 수량과 인벤토리의 실제 수량 중 작은 값만큼 제출
        int submitAmount = Mathf.Min(needed, actualItem.count);
        
        // 인벤토리에서 아이템 소모
        inven.ConsumeItemAt(temporaryItemIndex, submitAmount);
        submittedCount += submitAmount;
        
        // UI 업데이트
        UpdateQuestSlotUI(temporaryItem); 
        
        // ⭐ QuestManager에게 제출 완료 알림 (보상 지급 트리거)
        if (QuestManager.instance != null)
        {
            // 이 함수 호출 후 QuestSlot 오브젝트가 파괴되는지 확인이 필요합니다.
            QuestManager.instance.OnItemSubmitted(requiredItemName, submitAmount, this); 
        }
        
        ClearTemporarySlot(); // 임시 배치 상태 해제
        
        Debug.Log($"아이템 {requiredItemName} {submitAmount}개를 확정 납입했습니다. (총 {submittedCount}/{requiredAmount})");

        // 👇👇👇 중요: 여기서 파괴 여부 확인 👇👇👇
        if (this.gameObject == null)
        {
            // 이 로그가 출력되면 QuestManager.OnItemSubmitted 또는 그 외부에 의해 객체가 파괴된 것입니다.
            Debug.LogError("[QuestSlot] FATAL: ConfirmSubmission이 반환되기 직전에 오브젝트가 파괴되었습니다!"); 
        }
        // 👆👆👆 중요: 여기서 파괴 여부 확인 👆👆👆
        
        return true;
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