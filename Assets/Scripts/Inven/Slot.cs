using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IDropHandler
{
    public InventoryItem item;       // 슬롯이 보유한 아이템 데이터
    public Image itemIcon;           // 실제로 드래그/클릭 대상이 되는 아이콘 이미지
    public Text itemCountText;       // 아이템 개수 텍스트

    [HideInInspector]
    public int slotIndex;            // 슬롯 번호 (Inventory 배열 인덱스)

    private InventoryUI ui;          // InventoryUI 참조
    private Image slotBgImage;       // 슬롯 배경 이미지 (클릭 대상에서 제외)

    private void Awake()
    {
        // 부모에서 InventoryUI 검색
        ui = GetComponentInParent<InventoryUI>();

        // 슬롯 배경은 클릭/드래그 대상에서 제외 (표시만 함)
        slotBgImage = GetComponent<Image>();
        if (slotBgImage != null)
            slotBgImage.raycastTarget = false;

        // 아이콘/텍스트만 클릭·드래그 가능한 대상으로 설정
        if (itemIcon != null)
            itemIcon.raycastTarget = true;

        if (itemCountText != null)
            itemCountText.raycastTarget = true;

        // 초기 UI 비활성화
        ClearVisual();
    }

    // 아이콘 + 텍스트 비활성화
    private void ClearVisual()
    {
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        if (itemCountText != null)
            itemCountText.gameObject.SetActive(false);
    }

    // Slot UI 갱신 (아이템 아이콘/카운트)
    public void UpdateSlotUI()
    {
        if (item != null && item.itemImage != null)
        {
            itemIcon.sprite = item.itemImage;
            itemIcon.gameObject.SetActive(true);

            // 개수가 1개 초과일 때만 숫자 표시
            if (item.count > 1)
            {
                itemCountText.text = item.count.ToString();
                itemCountText.gameObject.SetActive(true);
            }
            else
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
        else
        {
            ClearVisual();
        }
    }

    // 슬롯 비우기
    public void RemoveSlot()
    {
        item = null;
        ClearVisual();
    }

    // 드래그된 아이템이 이 슬롯 위로 드랍되었을 때 호출됨
    public void OnDrop(PointerEventData eventData)
    {
        var drag = ItemDragHandler.currentlyDragging;
        if (drag == null) return;

        Slot fromSlot = drag.slot;
        if (fromSlot == null || fromSlot == this) return;

        Inventory inven = Inventory.instance;
        if (inven == null) return;

        int fromIdx = fromSlot.slotIndex;
        int toIdx   = slotIndex;

        // 인덱스 유효성 검사
        if (fromIdx < 0 || toIdx < 0) return;
        if (fromIdx >= ui.slots.Length || toIdx >= ui.slots.Length) return;

        // 빈 슬롯으로 드랍하는 것 금지
        if (inven.items[toIdx] == null)
            return;

        // 필요한 경우 리스트 크기 확장
        while (inven.items.Count <= toIdx)
            inven.items.Add(null);

        // 슬롯 아이템 교환
        var temp = inven.items[fromIdx];
        inven.items[fromIdx] = inven.items[toIdx];
        inven.items[toIdx] = temp;

        // UI 갱신 이벤트 호출
        inven.onChangeItem?.Invoke();
    }
}
