using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

// ExchangeSubmitSlot은 아이템 드래그/드롭을 처리하는 '진짜 슬롯'입니다.
// Slot 클래스를 상속받는다고 가정합니다.
public class ExchangeSubmitSlot : Slot
{
    // ⭐ 핵심 수정: public 필드를 private으로 전환하여 유니티 직렬화 간섭을 방지합니다. ⭐
    private string requiredItemName;
    private int requiredAmount;
    
    // ⭐ 현재 선택된 레시피 데이터 전체를 저장하여 ConfirmSubmission 시 사용합니다.
    public QuestData currentRecipeData; // QuestData 객체 참조는 public으로 유지

    [Header("Exchange Status")]
    // 제출 버튼을 누르기 전, 임시 배치된 아이템 정보
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1; 
    
    // UI 표시용 Text (Slot 클래스에 itemCountText가 있다고 가정)

    /// <summary>
    /// ExchangeRecipeItem을 선택했을 때, 이 슬롯의 요구 사항을 설정합니다.
    /// </summary>
    public void SetRequiredData(QuestData recipe) 
    {
        if (recipe == null)
        {
            ClearSlot();
            return;
        }

        this.currentRecipeData = recipe; // 레시피 데이터 저장
        
        // ⭐ private 필드에 값 할당
        this.requiredItemName = recipe.requiredItemName;
        this.requiredAmount = recipe.requiredAmount;
        
        ClearSlot(); // 새 레시피가 선택되었으므로 기존 임시 상태 초기화
        
        // 아이콘도 요구 아이템 아이콘으로 설정합니다.
        if (itemIcon != null && recipe.requiredItemIcon != null)
        {
            itemIcon.sprite = recipe.requiredItemIcon;
            itemIcon.gameObject.SetActive(true);
        }
        
        // ⭐ 이제 private 필드에 할당된 유효한 값으로 UI 업데이트
        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {this.requiredAmount}"; // 0개 제출 / 요구 수량
            itemCountText.gameObject.SetActive(true);
        }
        
        Debug.Log($"[ExchangeSubmitSlot] 요구 사항 설정: {this.requiredItemName} x {this.requiredAmount}");
    }
    
    /// <summary>
    /// 슬롯 상태를 초기화하고 UI를 숨깁니다.
    /// </summary>
    public void ClearSlot()
    {
        this.requiredItemName = null;
        this.requiredAmount = 0;
        this.currentRecipeData = null; // 레시피 데이터도 초기화
        
        temporaryItem = null;
        temporaryItemIndex = -1;
        
        // UI 초기화 (Slot 클래스의 itemIcon과 itemCountText 사용 가정)
        if (itemIcon != null) 
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        if (itemCountText != null) itemCountText.gameObject.SetActive(false);
    }


    // 아이템 드롭 처리: 요구 아이템인지, 수량이 충분한지 확인
    public override void OnDrop(PointerEventData eventData)
    {
        // 1. 현재 요구 아이템이 설정되어 있는지 확인 (private 필드 사용)
        if (string.IsNullOrEmpty(requiredItemName) || requiredAmount <= 0 || currentRecipeData == null)
        {
            // ⭐ 이제 이 로그는 public 필드 간섭 없이 순수하게 데이터 유효성을 나타냅니다.
            Debug.LogWarning($"[ExchangeSubmitSlot] 요구 레시피가 선택되지 않았거나 설정되지 않았습니다. (Name:{requiredItemName}, Amount:{requiredAmount})");
            return;
        }
        
        // 2. 드래그된 아이템 정보 확인
        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;
        Slot fromSlot = drag.slot; 
        if (fromSlot == null || fromSlot == this) return;
        
        Inventory inven = Inventory.instance;
        if (inven == null) 
        {
            Debug.LogError("[ExchangeSubmitSlot] Inventory.instance가 null입니다.");
            return;
        }
        
        int fromIdx = fromSlot.slotIndex;
        InventoryItem draggedItem = inven.items.Count > fromIdx ? inven.items[fromIdx] : null;
        
        // 3. 요구 아이템과 일치하는지 확인
        if (draggedItem == null || draggedItem.itemName != requiredItemName) // private 필드 사용
        {
            Debug.LogWarning($"[Exchange] 요구 아이템({requiredItemName})이 아닙니다. 드롭된 아이템: {(draggedItem != null ? draggedItem.itemName : "없음")}");
            return;
        }
        
        // 4. 임시 배치 상태 업데이트
        temporaryItem = draggedItem;
        temporaryItemIndex = fromIdx;
        UpdateTemporarySlotUI(draggedItem);
        Debug.Log($"아이템 {requiredItemName}이 제출 슬롯에 임시 배치되었습니다.");
    }
    
    /// <summary>
    /// 제출 버튼이 눌렸을 때 임시 배치된 아이템을 확정 제출하는 함수.
    /// ExchangeManager에서 호출되며, 실제 교환 로직을 수행합니다.
    /// </summary>
    // ⭐ ExchangeManager가 이 함수를 호출하여 아이템 제출을 시도합니다.
    public bool ConfirmSubmission()
    {
        // 슬롯이 이미 currentRecipeData를 가지고 있으므로 인자 없이 호출하도록 수정합니다.
        QuestData recipeToTrade = currentRecipeData;
        
        if (recipeToTrade == null)
        {
            Debug.LogWarning("[Exchange] 교환할 레시피 정보가 슬롯에 설정되지 않았습니다.");
            return false;
        }

        if (temporaryItem == null || temporaryItemIndex == -1)
        {
            Debug.LogWarning("[Exchange] 슬롯에 납입할 아이템이 없습니다.");
            return false;
        }
        
        Inventory inven = Inventory.instance;
        if (inven == null) return false;
        
        // 현재 인벤토리의 실제 아이템 정보
        InventoryItem actualItem = inven.items[temporaryItemIndex];
        
        // 레시피가 요구하는 수량 (private 필드 사용)
        int needed = this.requiredAmount; 
        
        // 제출 슬롯에 드롭한 아이템의 전체 수량
        int droppedAmount = actualItem.count;

        // 제출 가능 여부 확인
        if (actualItem == null || actualItem.itemName != requiredItemName) // private 필드 사용
        {
             Debug.LogError("[Exchange] 인벤토리 아이템 불일치. 슬롯에 임시 배치된 아이템이 인벤토리에서 사라졌을 수 있습니다.");
             ClearSlot();
             return false;
        }
        
        // 인벤토리의 아이템 수량이 요구 수량보다 적으면 실패
        if (droppedAmount < needed)
        {
            Debug.LogWarning($"[Exchange] 요구 수량({needed}개)보다 적은 {droppedAmount}개가 배치되었습니다.");
            // 이 경우, 슬롯을 초기화하고 사용자에게 알리는 UI 메시지가 필요합니다.
            ClearSlot(); 
            return false;
        }
        
        // --- 제출 성공 로직 ---
        
        // 인벤토리에서 요구 수량만큼 소모
        inven.ConsumeItemAt(temporaryItemIndex, needed);
        
        // 보상 지급 
        inven.AddItem(recipeToTrade.rewardItem, recipeToTrade.rewardCount);
        
        Debug.Log($"[Exchange] 교환 성공: {recipeToTrade.requiredItemName} {needed}개를 소모하고 {recipeToTrade.rewardItem.itemName} {recipeToTrade.rewardCount}개를 받았습니다.");
        
        ClearSlot(); // 슬롯 초기화
        
        // ExchangeManager에게 모든 레시피 상태 업데이트를 요청
        ExchangeManager.Instance.NotifyTradeSuccess(); 

        return true;
    }


    // 임시 배치된 아이템의 정보(개수)를 보여주는 UI 업데이트
    private void UpdateTemporarySlotUI(InventoryItem itemData)
    {
        // itemIcon과 itemCountText는 Slot 클래스에 정의되어 있다고 가정합니다.
        // requiredIcon은 SetRequiredData에서 이미 설정되었을 것입니다.
        if (itemIcon != null)
        {
            // itemIcon.sprite = itemData.itemImage; // 요구 아이템 아이콘을 유지합니다.
            itemIcon.gameObject.SetActive(true);
        }
        if (itemCountText != null)
        {
            // 드래그된 아이템의 전체 수량을 표시하여 요구 수량과 비교할 수 있도록 함
            itemCountText.text = $"x {itemData.count}"; 
            itemCountText.gameObject.SetActive(true);
        }
    }
}