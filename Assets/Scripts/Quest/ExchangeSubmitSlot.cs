using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExchangeSubmitSlot : Slot
{
    [HideInInspector] public QuestData recipe;

    public string requiredItemName;
    public int requiredAmount;

    public int submittedCount = 0;

    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1;

    public void SetRequiredData(QuestData recipe)
{
    SetRecipe(recipe);
}


    // ------------------------------
    //   레시피 설정
    // ------------------------------
    public void SetRecipe(QuestData data)
    {
        if (data == null)
        {
            ResetSlot();
            return;
        }

        recipe = data;

        requiredItemName = data.requiredItemName;
        requiredAmount = data.requiredAmount;

        submittedCount = 0;
        temporaryItem = null;
        temporaryItemIndex = -1;

        if (itemIcon != null && data.requiredItemIcon != null)
        {
            itemIcon.sprite = data.requiredItemIcon;
            itemIcon.gameObject.SetActive(true);
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
    }

    // ------------------------------
    //   ClearSlot — UI 초기화만 담당
    // ------------------------------
    public void ClearSlot()
    {
        temporaryItem = null;
        temporaryItemIndex = -1;

        submittedCount = 0;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemCountText != null)
        {
            itemCountText.text = "";
            itemCountText.gameObject.SetActive(false);
        }
    }

    // ------------------------------
    //   드랍
    // ------------------------------
    public override void OnDrop(PointerEventData eventData)
    {
        if (recipe == null)
        {
            Debug.Log("[ExchangeSubmitSlot] 레시피 없음");
            return;
        }

        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;
        Slot fromSlot = drag.slot;
        if (fromSlot == null || fromSlot == this) return;

        Inventory inven = Inventory.instance;
        InventoryItem item = inven.items[fromSlot.slotIndex];

        if (item == null || item.itemName != requiredItemName)
        {
            Debug.LogWarning("[ExchangeSubmitSlot] 요구 아이템과 불일치");
            return;
        }

        temporaryItem = item;
        temporaryItemIndex = fromSlot.slotIndex;

        if (itemCountText != null)
            itemCountText.text = $"임시: {item.count}";
    }

    // ------------------------------
    //   제출 확정
    // ------------------------------
    public bool ConfirmSubmission()
    {
        if (recipe == null)
        {
            Debug.LogWarning("[Exchange] 레시피 없음");
            return false;
        }

        if (temporaryItem == null)
        {
            Debug.LogWarning("[Exchange] 임시 아이템 없음");
            return false;
        }

        Inventory inven = Inventory.instance;
        InventoryItem item = inven.items[temporaryItemIndex];

        int need = requiredAmount - submittedCount;
        int give = Mathf.Min(need, item.count);

        inven.ConsumeItemAt(temporaryItemIndex, give);

        submittedCount += give;

        // UI 업데이트
        if (itemCountText != null)
            itemCountText.text = $"{submittedCount} / {requiredAmount}";

        temporaryItem = null;
        temporaryItemIndex = -1;

        if (submittedCount >= requiredAmount)
        {
            inven.AddItem(recipe.rewardItem, recipe.rewardCount);
            ExchangeManager.Instance.NotifyTradeSuccess();
        }

        return true;
    }

    // ------------------------------
    //   완전 초기화
    // ------------------------------
    public void ResetSlot()
    {
        recipe = null;
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
        {
            itemCountText.gameObject.SetActive(false);
        }
    }
}
