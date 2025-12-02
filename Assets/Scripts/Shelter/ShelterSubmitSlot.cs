using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 쉘터용 납입 슬롯
/// </summary>
public class ShelterSubmitSlot : Slot, IDropHandler
{
    [HideInInspector] public QuestData recipe;
    public string requiredItemName;
    public int requiredAmount;
    public int submittedCount = 0;

    [HideInInspector] public InventoryItem temporaryItem = null;
    [HideInInspector] public int temporaryItemIndex = -1;

    // Slot UI
    public Image itemIcon;
    public Text itemCountText;

    // 슬롯 초기화
    public void SetRequiredData(QuestData data)
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

        if (itemIcon != null)
        {
            itemIcon.sprite = data.requiredItemIcon;
            itemIcon.gameObject.SetActive(data.requiredItemIcon != null);
            itemIcon.color = Color.white;
        }

        if (itemCountText != null)
        {
            itemCountText.text = $"0 / {requiredAmount}";
            itemCountText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 드래그한 아이템 슬롯에서 Drop될 때 처리
    /// </summary>
    public override void OnDrop(PointerEventData eventData)
    {
        if (recipe == null) return;

        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null || drag.slot == null) return;

        Slot fromSlot = drag.slot;
        InventoryItem item = Inventory.instance?.items[fromSlot.slotIndex];

        if (item == null || item.itemName != requiredItemName)
        {
            Debug.LogWarning("[ShelterSubmitSlot] 요구 아이템과 불일치");
            return;
        }

        // 임시 저장
        temporaryItem = item;
        temporaryItemIndex = fromSlot.slotIndex;

        // UI 업데이트
        if (itemCountText != null)
            itemCountText.text = $"{item.count} / {requiredAmount}";
    }

    /// <summary>
    /// 제출 확인
    /// </summary>
    public bool ConfirmSubmission()
    {
        if (recipe == null || temporaryItem == null || Inventory.instance == null)
            return false;

        InventoryItem item = Inventory.instance.items[temporaryItemIndex];
        if (item == null) return false;

        int need = requiredAmount - submittedCount;
        int give = Mathf.Min(need, item.count);

        Inventory.instance.ConsumeItemAt(temporaryItemIndex, give);
        submittedCount += give;

        if (itemCountText != null)
            itemCountText.text = $"{submittedCount} / {requiredAmount}";

        temporaryItem = null;
        temporaryItemIndex = -1;

        if (submittedCount >= requiredAmount)
        {
            Debug.Log($"[ShelterSubmitSlot] '{recipe.questName}' 납입 완료");

            if (itemIcon != null)
                itemIcon.color = Color.gray;

            if (itemCountText != null)
                itemCountText.text = "납입 완료";
        }

        return true;
    }

    /// <summary>
    /// 슬롯 리셋
    /// </summary>
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
