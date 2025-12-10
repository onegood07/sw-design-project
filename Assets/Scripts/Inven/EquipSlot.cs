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
        // 인스펙터에서 비워둔 경우, 같은 오브젝트의 Slot 컴포넌트에서 가져온다.
        if (itemIcon == null)
        {
            Slot slot = GetComponent<Slot>();
            if (slot != null && slot.itemIcon != null)
            {
                itemIcon = slot.itemIcon;
            }
        }

        // 그래도 null이면, 자식 중 첫 번째 Image 를 사용한다.
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

    private void Start()
    {
        // InventoryManager 에 저장된 장비 정보를 이용해 상태를 복원
        RestoreFromInventoryManager();
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

        // 인벤토리 아이템을 이 장비 슬롯에 장착
        equippedItem = fromItem;

        // 장비 상태를 InventoryManager 에도 저장
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.equippedItemsByType[(int)acceptedType] = equippedItem;
        }

        if (itemIcon != null)
        {
            ApplyIcon(fromItem.itemImage);
            Debug.Log("[EquipSlot] 아이콘 세팅 완료.", this);
        }
        else
        {
            Debug.LogWarning("[EquipSlot] itemIcon 이 null 입니다. 인스펙터에서 Item Icon 을 연결했는지 확인하세요.", this);
        }

        //  - 신발(Shoes): Heal 타입 아이템을 신발 슬롯(acceptedType == ItemView.Heal)에 장착하면
        //    이동 속도 버프를 주는 MedicineData.Use를 자동으로 한 번 호출한다.
        if (acceptedType == ItemView.Heal && fromItem.itemData is IUsable equipUsable)
        {
            Transform heroTransform = HeroStat.Instance != null ? HeroStat.Instance.transform : null;
            Vector2 viewDir = Vector2.down;

            if (HeroMoveControl.Instance != null)
            {
                viewDir = HeroMoveControl.Instance.CurrentViewDirection;
            }

            if (heroTransform != null)
            {
                equipUsable.Use(heroTransform, viewDir);
            }
        }

        // 인벤토리 슬롯에서 아이템 제거 (이동 느낌 나게)
        Inventory inven = Inventory.instance;
        if (inven != null)
        {
            int idx = fromSlot.slotIndex;
            if (idx >= 0 && idx < inven.items.Count)
            {
                // 자동 발동이 필요한 경우(예: 랜턴)에는 Use 호출
                if (acceptedType == ItemView.Lantern && fromItem.itemData is IUsable autoUsable)
                {
                    autoUsable.Use(HeroStat.Instance.transform, Vector2.zero);
                }

                // 인벤토리 데이터에서 해당 슬롯 비우기
                inven.items[idx] = null;

                // 슬롯 자체도 즉시 비워서 UI 상으로도 "빈 칸" 이 되도록 처리
                fromSlot.RemoveSlot();

                // 인벤토리 변경 이벤트 브로드캐스트 (다른 UI들이 함께 갱신되도록)
                inven.onChangeItem?.Invoke();
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
            // 0 ~ slotCnt-1 범위에서 가장 앞의 빈 칸을 찾는다.
            bool placed = false;
            int maxSlot = Mathf.Max(0, inven.slotCnt);

            for (int i = 0; i < maxSlot; i++)
            {
                // 리스트가 짧으면 null 로 채워 길이 보정
                while (inven.items.Count <= i)
                {
                    inven.items.Add(null);
                }

                if (inven.items[i] == null)
                {
                    inven.items[i] = equippedItem;
                    placed = true;
                    break;
                }
            }

            // 0~slotCnt-1 안에 빈 칸이 없으면 리스트 끝에 추가한다.
            if (!placed)
            {
                inven.AddInventoryItemInstance(equippedItem);
            }

            // 인벤토리 변경 사항을 UI에 반영
            inven.onChangeItem?.Invoke();
        }

        // 장비 해제 시 InventoryManager 정보도 제거
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.equippedItemsByType[(int)acceptedType] = null;
        }

        // 장비 해제 시 효과 제거
        if (acceptedType == ItemView.Lantern && equippedItem.itemData is IUsable lanternUsable)
        {
            lanternUsable.Use(HeroStat.Instance.transform, Vector2.zero);
        }
        // - 신발(아이템 ID 301): 남아 있는 이동속도 버프를 즉시 제거
        else if (equippedItem.itemData.getItemName == 301 && HeroStat.Instance != null)
        {
            HeroStat.Instance.CancelEquipSpeedBoost();
        }

        equippedItem = null;

        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);
    }

    // InventoryManager 에 저장된 장비 정보를 이용해 EquipSlot UI를 복원
    private void RestoreFromInventoryManager()
    {
        if (InventoryManager.Instance == null)
            return;

        var mgr = InventoryManager.Instance;
        int typeIndex = (int)acceptedType;

        if (typeIndex < 0 || typeIndex >= mgr.equippedItemsByType.Length)
            return;

        InventoryItem savedItem = mgr.equippedItemsByType[typeIndex];
        if (savedItem == null)
        {
            // 저장된 장비가 없으면 아이콘만 숨긴 상태로 둔다.
            if (itemIcon != null)
                itemIcon.gameObject.SetActive(false);
            return;
        }

        equippedItem = savedItem;

        // 아이콘 스프라이트 갱신
        if (itemIcon != null)
        {
            ApplyIcon(equippedItem.itemImage);
        }
    }
}


