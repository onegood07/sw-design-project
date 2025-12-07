using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 UI 한 칸을 나타내며 아이콘 표시/드랍 교환을 처리합니다.
/// </summary>
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
    /// <summary>
    /// 아이콘과 개수 표기를 숨겨 빈 슬롯 상태로 만듭니다.
    /// </summary>
    private void ClearVisual()
    {
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        if (itemCountText != null)
            itemCountText.gameObject.SetActive(false);
    }

    // Slot UI 갱신 (아이템 아이콘/카운트)
    /// <summary>
    /// 슬롯에 할당된 아이템 정보를 UI 위젯에 반영합니다.
    /// </summary>
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
    /// <summary>
    /// 슬롯 데이터를 비우고 UI를 초기 상태로 돌립니다.
    /// </summary>
    public void RemoveSlot()
    {
        item = null;
        ClearVisual();
    }

    // 드래그된 아이템이 이 슬롯 위로 드랍되었을 때 호출됨
    /// <summary>
    /// 다른 슬롯에서 드래그된 아이템을 받아 이동/교환합니다.
    /// </summary>
    public virtual void OnDrop(PointerEventData eventData)
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

        // 필요한 경우 리스트 크기 확장
        while (inven.items.Count <= Mathf.Max(fromIdx, toIdx))
            inven.items.Add(null);

        // 더 이상 "스왑"은 허용하지 않고, 비어 있는 슬롯으로만 이동시킨다.
        // 대상 슬롯이 비어 있으면 fromIdx → toIdx 로 이동, 아니면 아무 일도 하지 않음.
        if (inven.items[toIdx] == null)
        {
            inven.items[toIdx] = inven.items[fromIdx];
            inven.items[fromIdx] = null;

            // 인벤토리 데이터 변경을 알리고, InventoryUI.RedrawSlotUI 쪽에서
            // 모든 슬롯의 item / 아이콘을 다시 그리도록 맡긴다.
            inven.onChangeItem?.Invoke();
        }
    }
}
