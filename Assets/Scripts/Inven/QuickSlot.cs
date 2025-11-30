using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 퀵슬롯(QuickSlot1~6)에 붙일 전용 스크립트.
/// 인벤토리 슬롯에서 드래그한 아이템을 이 퀵슬롯에 등록해서
/// 나중에 단축키(1~4번 등)로 사용할 수 있게 하기 위한 기본 구조입니다.
/// </summary>
public class QuickSlot : MonoBehaviour
{
    [Header("퀵슬롯 인덱스 (0~5)")]
    public int quickIndex;                  // QuickSlot1 = 0, QuickSlot2 = 1, ...

    [Header("UI 참조")]
    public Image itemIcon;                  // 퀵슬롯에 표시될 아이콘
    public Text itemCountText;             // 수량 표시 텍스트(선택)

    [Header("아이콘 표시 설정")]
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);

    [HideInInspector]
    public InventoryItem linkedItem;        // 이 퀵슬롯과 연결된 인벤토리 아이템
    [HideInInspector]
    public ItemData linkedItemData;
    [HideInInspector]
    public int linkedItemCount;
    [HideInInspector]
    public int linkedInventoryIndex = -1;

    private static readonly System.Collections.Generic.List<QuickSlot> allSlots
        = new System.Collections.Generic.List<QuickSlot>();

    private void Awake()
    {
        if (!allSlots.Contains(this))
            allSlots.Add(this);

        // 처음에는 아이콘 숨김
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
            itemIcon.raycastTarget = false;
        }
        if (itemCountText != null)
        {
            itemCountText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        allSlots.Remove(this);
    }

    public static QuickSlot FindSlotUnderPointer(Vector2 screenPos, Camera uiCamera)
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;

            RectTransform slotRect = slot.transform as RectTransform;
            if (slotRect != null && slotRect.rect.size.sqrMagnitude > 0f &&
                RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos, uiCamera))
            {
                return slot;
            }

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

    public static QuickSlot GetSlotByIndex(int index)
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null) continue;
            if (slot.quickIndex == index)
                return slot;
        }
        return null;
    }

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
        itemIcon.transform.SetAsLastSibling();
        var slotRect = transform as RectTransform;
        if (slotRect != null && itemIcon.transform.GetSiblingIndex() <= slotRect.GetSiblingIndex())
        {
            itemIcon.transform.SetSiblingIndex(slotRect.GetSiblingIndex() + 1);
        }

        UpdateCountDisplay();
    }

    private void UpdateCountDisplay()
    {
        if (itemCountText == null) return;

        if (linkedItemCount > 1)
        {
            itemCountText.text = linkedItemCount.ToString();
            itemCountText.gameObject.SetActive(true);
        }
        else
        {
            itemCountText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 인벤토리의 Slot에서 끌어온 아이템을 이 퀵슬롯에 등록하는 함수.
    /// Drag 끝났을 때 ItemDragHandler 에서 직접 호출한다.
    /// </summary>
    public void AssignFromSlot(Slot fromSlot)
    {
        if (fromSlot == null || fromSlot.item == null) return;

        InventoryItem fromItem = fromSlot.item;

        // 필요하면 타입 제한도 가능 (예: 힐 아이템만 올리기)
        // if (fromItem.itemType != ItemView.Heal) return;

        linkedItem = fromItem;
        linkedItemData = fromItem.itemData;
        linkedInventoryIndex = fromSlot.slotIndex;
        linkedItemCount = linkedItem != null ? linkedItem.count : fromItem.count;

        ApplyIcon(fromItem.itemImage);

        if (linkedItemData != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.setQuickSlot(linkedItemData, quickIndex, linkedItemCount, linkedInventoryIndex);
        }
        else
        {
            Debug.LogWarning($"[QuickSlot] ItemData 가 없어 퀵슬롯 {quickIndex + 1}에 등록되지 않았습니다.", this);
        }

        // 이후에 필요하면:
        // - Inventory.instance.quickSlots[quickIndex] 에도 함께 저장 (InventoryManager 연동)
        // - 단축키 입력 시 linkedItem 을 사용하는 로직 연결
    }

    public void UpdateLinkedCount(int newCount)
    {
        linkedItemCount = newCount;
        if (linkedItem != null)
            linkedItem.count = newCount;
        UpdateCountDisplay();
        if (linkedItemCount <= 0)
        {
            ClearSlotVisual();
        }
    }

    public void ClearSlotVisual()
    {
        linkedItem = null;
        linkedItemData = null;
        linkedItemCount = 0;
        linkedInventoryIndex = -1;
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);
        if (itemCountText != null)
            itemCountText.gameObject.SetActive(false);
    }
}


