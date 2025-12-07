using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 아이템을 드래그해서 버리는 전용 영역.
/// - 캔버스 안에 Image 등 UI 오브젝트를 만들고 이 스크립트를 붙여두면,
///   인벤토리 슬롯 아이콘을 드래그해서 이 영역 위에 놓았을 때 해당 아이템 스택을 삭제합니다.
/// </summary>
public class InventoryTrashZone : MonoBehaviour, IDropHandler
{
    /// <summary>
    /// 드래그가 이 영역 위에서 끝났을 때 호출됩니다.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // PointerEvent 로부터 실제 드래그 중이던 아이콘을 찾는다.
        if (eventData == null || eventData.pointerDrag == null)
            return;

        var dragHandler = eventData.pointerDrag.GetComponent<ItemDragHandler>();
        if (dragHandler == null || dragHandler.slot == null)
            return;

        Inventory inven = Inventory.instance;
        if (inven == null)
            return;

        // 드래그했던 슬롯의 인덱스를 그대로 사용해서,
        // 그 슬롯에 들어 있는 아이템 스택만 삭제한다.
        int index = dragHandler.slot.slotIndex;
        if (index < 0 || index >= inven.items.Count)
            return;

        var invItem = inven.items[index];
        if (invItem != null && invItem.count > 0)
        {
            inven.ConsumeItemAt(index, invItem.count);
        }

        // 시각적으로 떠 있는 드래그 아이콘을 원래 슬롯 위치로 돌려놓는다.
        // (이후 인벤토리 UI 갱신으로 해당 아이콘은 숨겨짐)
        dragHandler.RestoreToOriginal();
    }
}

