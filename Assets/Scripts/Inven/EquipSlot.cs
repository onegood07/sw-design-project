using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 장비 슬롯 (LanternSlot, ShoesSlot, WeaponSlot)에 붙일 스크립트.
/// 인벤토리 슬롯(ScrollView 안의 Content 하위 슬롯)에서 드래그한 아이템을
/// 타입에 맞게 받아와서 아이콘만 표시하는 기본 구조입니다.
/// </summary>
public class EquipSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("이 슬롯이 받을 수 있는 아이템 타입")]
    public ItemView acceptedType;        // 예: Lantern, Weapon 등

    [Header("UI 참조")]
    public Image itemIcon;              // 장착된 아이템 아이콘 (비워두면 자동으로 Slot.itemIcon 사용)

    [Header("아이콘 표시 설정")]
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);

    [HideInInspector]
    public InventoryItem equippedItem;  // 현재 장착된 아이템 데이터

    private static readonly System.Collections.Generic.List<EquipSlot> allSlots
        = new System.Collections.Generic.List<EquipSlot>();

    private void TryAutoAssignItemIcon()
    {
        // 1) 인스펙터에서 비워둔 경우, 같은 오브젝트의 Slot 컴포넌트에서 시도
        if (itemIcon == null)
        {
            Slot slot = GetComponent<Slot>();
            if (slot != null && slot.itemIcon != null)
            {
                itemIcon = slot.itemIcon;
            }
        }

        // 2) 그래도 null이면, 자식 중 첫 번째 Image 를 자동으로 사용 (주로 ItemImage)
        if (itemIcon == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != this.gameObject)
                {
                    itemIcon = img;
                    break;
                }
            }
        }
    }

    private void Awake()
    {
        if (!allSlots.Contains(this))
            allSlots.Add(this);

        // 자동 할당 시도
        TryAutoAssignItemIcon();

        // 장착 전에는 아이콘 숨김
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
            itemIcon.raycastTarget = true;
        }
    }

    private void OnDestroy()
    {
        allSlots.Remove(this);
    }

    /// <summary>
    /// 현재 포인터 위치에 있는 장비 슬롯을 찾아 드래그 시 사용합니다.
    /// </summary>
    public static EquipSlot FindSlotUnderPointer(Vector2 screenPos, Camera uiCamera)
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;

            // 1) 슬롯 자체 Rect 검사
            RectTransform slotRect = slot.transform as RectTransform;
            if (slotRect != null && slotRect.rect.size.sqrMagnitude > 0f &&
                RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos, uiCamera))
            {
                return slot;
            }

            // 2) 아이콘 Rect 검사 (슬롯 Rect 크기가 0인 경우 대비)
            if (slot.itemIcon != null)
            {
                RectTransform iconRect = slot.itemIcon.rectTransform;
                if (iconRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(iconRect, screenPos, uiCamera))
                {
                    return slot;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 장착된 아이템의 스프라이트를 슬롯 아이콘에 반영합니다.
    /// </summary>
    private void ApplyIcon(Sprite sprite)
    {
        if (itemIcon == null || sprite == null) return;

        itemIcon.sprite = sprite;
        itemIcon.gameObject.SetActive(true);

        RectTransform iconRect = itemIcon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = iconSize;

        itemIcon.preserveAspect = true;
        itemIcon.transform.SetAsLastSibling();    // 배경보다 위에 표시
        // 슬롯 자체보다도 항상 앞에 오도록 보장
        var slotRect = transform as RectTransform;
        if (slotRect != null && itemIcon.transform.GetSiblingIndex() <= slotRect.GetSiblingIndex())
        {
            itemIcon.transform.SetSiblingIndex(slotRect.GetSiblingIndex() + 1);
        }
    }

    /// <summary>
    /// 인벤토리의 Slot에서 끌어온 아이템을 이 장비 슬롯에 장착시키는 함수.
    /// Drag 끝났을 때 ItemDragHandler 에서 직접 호출한다.
    /// </summary>
    public void EquipFromSlot(Slot fromSlot)
    {
        // 드래그 시작한 쪽은 인벤토리의 Slot 이어야 함
        if (fromSlot == null)
        {
            Debug.LogWarning("[EquipSlot] EquipFromSlot: fromSlot 이 null 입니다.", this);
            return;
        }
        if (fromSlot.item == null)
        {
            Debug.LogWarning("[EquipSlot] EquipFromSlot: fromSlot.item 이 null 입니다. (비어있는 슬롯에서 드래그)", this);
            return;
        }

        InventoryItem fromItem = fromSlot.item;
        Debug.Log($"[EquipSlot] EquipFromSlot 호출됨. 아이템: {fromItem.itemName}, 타입: {fromItem.itemType}", this);

        // 타입이 맞는 아이템만 장착
        if (acceptedType != fromItem.itemType)
        {
            Debug.LogWarning($"[EquipSlot] EquipFromSlot: 타입 불일치. 이 슬롯: {acceptedType}, 아이템 타입: {fromItem.itemType}", this);
            return;
        }

        // 일단은 "복사해서 장착" 개념으로, 인벤토리 쪽 수량 변화는 아직 건드리지 않음.
        equippedItem = fromItem;

        if (itemIcon != null)
        {
            ApplyIcon(fromItem.itemImage);
            Debug.Log("[EquipSlot] 아이콘 세팅 완료.", this);
        }
        else
        {
            Debug.LogWarning("[EquipSlot] itemIcon 이 null 입니다. 인스펙터에서 Item Icon 을 연결했는지 확인하세요.", this);
        }

        // 인벤토리 슬롯에서 아이템 제거 (이동 느낌 나게)
        Inventory inven = Inventory.instance;
        if (inven != null)
        {
            int idx = fromSlot.slotIndex;
            if (idx >= 0 && idx < inven.items.Count)
            {
                // 장착하자 마자 사용 되도록
                if(inven.items[idx].itemData is IUsable usable)usable.Use(HeroStat.Instance.transform,Vector2.zero);
                
                inven.items[idx] = null;
                inven.onChangeItem?.Invoke();   // 인벤 UI 다시 그리기
                Debug.Log($"[EquipSlot] 인벤토리 {idx}번 슬롯 아이템 제거 완료.", this);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        UnequipToInventory();
    }

    void UnequipToInventory()
    {
        if (equippedItem == null)
            return;

        Inventory inven = Inventory.instance;
        if (inven != null)
        {
            inven.AddInventoryItemInstance(equippedItem);
        }

        equippedItem = null;

        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);
    }
}


