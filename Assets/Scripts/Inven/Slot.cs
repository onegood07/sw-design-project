using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;   // ← TextMeshPro 사용 시

public class Slot : MonoBehaviour, IDropHandler
{
    public InventoryItem item;   
    public Image itemIcon;       
    public TextMeshProUGUI itemCountText;   // ← 추가

    [HideInInspector]
    public int slotIndex;

    public void UpdateSlotUI()
    {
        if (item != null && item.itemImage != null)
        {
            // 아이콘 표시
            itemIcon.sprite = item.itemImage;
            itemIcon.gameObject.SetActive(true);

            // ★ 여기서 개수 표시
            if (item.count > 1)
            {
                itemCountText.text = item.count.ToString();
                itemCountText.gameObject.SetActive(true);
            }
            else
            {
                // 1개면 굳이 숫자 안 보이게
                itemCountText.gameObject.SetActive(false);
            }
        }
        else
        {
            itemIcon.gameObject.SetActive(false);
            itemCountText.gameObject.SetActive(false);
        }
    }

    public void RemoveSlot()
    {
        item = null;
        itemIcon.gameObject.SetActive(false);
        itemCountText.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;

        Slot fromSlot = drag.slot;
        if (fromSlot == null || fromSlot == this) return;

        Inventory inven = Inventory.instance;
        if (inven == null) return;

        int fromIdx = fromSlot.slotIndex;
        int toIdx = slotIndex;

        if (fromIdx < 0 || toIdx < 0) return;
        if (fromIdx >= inven.items.Count || toIdx >= inven.items.Count) return;

        // 데이터 스왑
        var temp = inven.items[fromIdx];
        inven.items[fromIdx] = inven.items[toIdx];
        inven.items[toIdx] = temp;

        // UI 갱신
        inven.onChangeItem?.Invoke();
    }
}
