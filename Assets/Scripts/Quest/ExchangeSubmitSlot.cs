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
            itemIcon.color = Color.white;
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
    }

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

    public override void OnDrop(PointerEventData eventData)
    {
        if (recipe == null) return;

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
            itemCountText.text = $"{item.count} / {requiredAmount}";
    }

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
            // 보상 지급
            inven.AddItem(recipe.rewardItem, recipe.rewardCount);
            ExchangeManager.Instance.NotifyTradeSuccess();

            // 슬롯 UI 비활성화
            if (itemCountText != null)
                itemCountText.text = "0 / 0";
            if (itemIcon != null)
                itemIcon.color = Color.gray;
        }

        return true;
    }

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
            itemCountText.gameObject.SetActive(false);
    }
}
