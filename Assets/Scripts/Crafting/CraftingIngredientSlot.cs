using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 조합 UI에서 특정 레시피의 한 가지 재료를 담당하는 슬롯입니다.
/// 플레이어가 인벤토리에서 재료를 드롭하면 임시로 보관하며 요구 수량을 확인합니다.
/// </summary>
public class CraftingIngredientSlot : Slot
{
    [Header("Ingredient Slot Data")]
    // 이 슬롯이 요구하는 아이템 정보 (RecipeData에서 복사해옴)
    public string requiredItemName;
    public int requiredAmount;

    // 현재 이 슬롯에 드롭된 아이템 정보
    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1;
    [HideInInspector] public int currentHeldCount = 0; // 이 슬롯에 드롭된 아이템의 총 개수

    // 슬롯이 연결된 메인 크래프팅 UI 또는 매니저를 참조해야 합니다.
    // 여기서는 UI의 상위 컴포넌트(예: CraftingMainPanel)에 재료 상태를 전달한다고 가정합니다.
    public System.Action<CraftingIngredientSlot> OnSlotUpdated; 

    /// <summary>
    /// 이 슬롯에 필요한 재료 정보와 수량을 설정합니다.
    /// </summary>
    public void SetRequiredData(string name, int amount, Sprite icon)
    {
        requiredItemName = name;
        requiredAmount = amount;
        ResetSlot(); // 이전 데이터 초기화

        if (itemIcon != null && icon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.gameObject.SetActive(true);
            itemIcon.color = Color.white;
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 슬롯 상태를 초기화합니다.
    /// </summary>
    public void ClearRequiredData()
    {
        requiredItemName = null;
        requiredAmount = 0;
        ResetSlot();
    }

    /// <summary>
    /// 드래그 앤 드롭 이벤트 처리.
    /// </summary>
    public override void OnDrop(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(requiredItemName)) return;

        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;
        Slot fromSlot = drag.slot;
        if (fromSlot == null || fromSlot == this) return;

        Inventory inven = Inventory.instance;
        InventoryItem item = inven.items[fromSlot.slotIndex];

        // 1. 요구 아이템과 이름이 일치하는지 확인
        if (item == null || item.itemName != requiredItemName)
        {
            Debug.LogWarning($"[CraftingIngredientSlot] 요구 아이템({requiredItemName})과 불일치");
            return;
        }

        // 2. 임시 보관 정보 업데이트
        temporaryItem = item;
        temporaryItemIndex = fromSlot.slotIndex;
        currentHeldCount = item.count; // 현재 드롭된 슬롯의 아이템 개수

        // 3. UI 업데이트
        UpdateSlotUI();
        
        // 4. 상위 UI에 상태 변경 통보
        OnSlotUpdated?.Invoke(this); 
    }

    /// <summary>
    /// 슬롯 UI를 현재 상태(드롭된 아이템 수량)로 업데이트합니다.
    /// </summary>
    public new void UpdateSlotUI() // ⭐ 'new' 키워드를 사용하여 경고를 제거합니다.
    {
        if (itemCountText != null)
        {
            if (temporaryItem != null)
            {
                // 인벤토리의 실제 수량으로 갱신
                Inventory inven = Inventory.instance;
                if (temporaryItemIndex != -1 && inven.items.Count > temporaryItemIndex && inven.items[temporaryItemIndex] != null)
                {
                    currentHeldCount = inven.items[temporaryItemIndex].count;
                }
                else
                {
                    // 인벤토리에서 아이템이 사라졌다면 슬롯 초기화
                    currentHeldCount = 0;
                    temporaryItem = null;
                    temporaryItemIndex = -1;
                }
            }
            else
            {
                currentHeldCount = 0;
            }
            
            // 색상으로 요구 수량 충족 여부 표시
            bool isEnough = currentHeldCount >= requiredAmount;
            itemCountText.color = isEnough ? Color.green : Color.white;
            itemCountText.text = $"{currentHeldCount} / {requiredAmount}";
        }
    }
    
    /// <summary>
    /// 이 슬롯에 충분한 재료가 드롭되었는지 확인합니다.
    /// </summary>
    public bool HasEnoughMaterial()
    {
        // 드롭된 아이템이 실제로 인벤토리에 남아있는지 확인
        if (temporaryItem == null || temporaryItemIndex == -1 || Inventory.instance.items[temporaryItemIndex] == null)
        {
            return false;
        }

        return currentHeldCount >= requiredAmount;
    }

    /// <summary>
    /// 조합 성공 후 이 슬롯의 임시 데이터를 비웁니다.
    /// </summary>
    public void ResetSlot()
    {
        temporaryItem = null;
        temporaryItemIndex = -1;
        currentHeldCount = 0;

        if (itemIcon != null)
        {
            // 요구 아이콘은 유지하고, 임시 보관 상태만 초기화
            itemIcon.color = Color.white;
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {requiredAmount}";
            itemCountText.color = Color.white;
        }
    }
}