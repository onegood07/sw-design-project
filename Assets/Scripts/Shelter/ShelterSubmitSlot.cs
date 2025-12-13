using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 쉘터용 납입 슬롯 (Item 컴포넌트 기반으로 수정)
/// </summary>
public class ShelterSubmitSlot : Slot, IDropHandler
{
    // Item 컴포넌트와 이름을 저장
    [HideInInspector] public Item requiredItem; 
    public string requiredItemName;
    
    public int requiredAmount;
    public int submittedCount = 0; // 이 슬롯 인스턴스가 가지는 제출 수량
    
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1;

    /// <summary>
    /// 슬롯 초기화 (Item 컴포넌트와 수량 기반으로 수정)
    /// </summary>
    public void SetRequiredData(Item itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            ResetSlot();
            return;
        }

        requiredItem = itemData; 
        requiredItemName = itemData.itemName; 
        requiredAmount = amount;
        
        // 이 함수 호출 시 submittedCount는 외부(GameManager)에서 반영됨
        submittedCount = 0; 
        temporaryItem = null;
        temporaryItemIndex = -1;

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.itemImage;
            itemIcon.gameObject.SetActive(itemData.itemImage != null);
            itemIcon.color = Color.white;
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 드래그한 아이템 슬롯에서 Drop될 때 처리
    /// </summary>
    public override void OnDrop(PointerEventData eventData)
    {
        if (requiredItem == null || submittedCount >= requiredAmount) return; // 납입 완료 시 드롭 불가

        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null || drag.slot == null) return;

        Slot fromSlot = drag.slot;
        InventoryItem item = Inventory.instance?.items[fromSlot.slotIndex]; 

        if (item == null || item.itemName != requiredItemName)
        {
            Debug.LogWarning($"[ShelterSubmitSlot] 요구 아이템({requiredItemName})과 불일치: 드롭된 아이템({item?.itemName})");
            return;
        }

        temporaryItem = item;
        temporaryItemIndex = fromSlot.slotIndex;
        
        // 드롭된 아이템의 전체 수량으로 텍스트를 임시 표시 (이전 로직 유지)
        if (itemCountText != null)
            itemCountText.text = $"{item.count} / {requiredAmount}";
    }

    /// <summary>
    /// 제출 확인
    /// </summary>
    public bool ConfirmSubmission()
    {
        if (requiredItem == null || temporaryItem == null || Inventory.instance == null)
            return false;

        InventoryItem item = Inventory.instance.items[temporaryItemIndex];
        if (item == null) return false;

        int need = requiredAmount - submittedCount;
        int give = Mathf.Min(need, item.count);

        if (give <= 0) return false; // 줄 아이템이 없거나 이미 완료된 경우

        // 인벤토리 아이템 소모
        Inventory.instance.ConsumeItemAt(temporaryItemIndex, give); 
        submittedCount += give;

        // GameManager의 제출 상태를 업데이트
        if (GameManager.Instance != null && GameManager.Instance.CurrentSubmittedData.ContainsKey(requiredItem))
        {
            GameManager.Instance.CurrentSubmittedData[requiredItem] = submittedCount;
        }

        // UI 갱신
        if (itemCountText != null)
            itemCountText.text = $"{submittedCount} / {requiredAmount}";

        temporaryItem = null;
        temporaryItemIndex = -1;

        if (submittedCount >= requiredAmount)
        {
            Debug.Log($"[ShelterSubmitSlot] '{requiredItemName}' 납입 완료");

            if (itemIcon != null)
                itemIcon.color = Color.gray;

            if (itemCountText != null)
                itemCountText.text = "완료";
            
            if (GameManager.Instance != null)
                GameManager.Instance.ShelterItemScore++; 
        }

        // 인벤토리에서 아이템을 소모했으므로, 빈 칸을 앞으로 당겨 정렬한다.
        if (Inventory.instance != null)
        {
            Inventory.instance.CompactItems();
        }

        return true;
    }

    /// <summary>
    /// 이전에 제출된 수량을 반영하고 UI를 갱신합니다.
    /// </summary>
    public void ReflectSubmittedCount(int count)
    {
        // 제출된 수량을 GameManager 데이터로 덮어씁니다.
        submittedCount = Mathf.Clamp(count, 0, requiredAmount); 
        
        // UI 업데이트
        if (itemCountText != null)
        {
            itemCountText.text = $"{submittedCount} / {requiredAmount}";
        }

        if (submittedCount >= requiredAmount)
        {
            // 납입 완료 상태 시 UI 시각 효과 적용
            if (itemIcon != null) itemIcon.color = Color.gray;
            if (itemCountText != null) itemCountText.text = "완료";
        }
    }

    /// <summary>
    /// 슬롯 리셋
    /// </summary>
    public void ResetSlot()
    {
        requiredItem = null;
        requiredItemName = null;
        requiredAmount = 0;
        submittedCount = 0;
        temporaryItem = null;
        temporaryItemIndex = -1;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemCountText != null)
            itemCountText.gameObject.SetActive(false);
    }
}