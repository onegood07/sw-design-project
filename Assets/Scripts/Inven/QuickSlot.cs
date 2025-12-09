using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 퀵슬롯(QuickSlot1~6)에 붙일 전용 스크립트.
/// 인벤토리 슬롯에서 드래그한 아이템을 이 퀵슬롯에 등록해서
/// 나중에 단축키(1~4번 등)로 사용할 수 있게 하기 위한 기본 구조입니다.
/// </summary>
public class QuickSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("퀵슬롯 인덱스 (0~5)")]
    public int quickIndex;                  // QuickSlot1 = 0, QuickSlot2 = 1, ...

    [Header("UI 참조")]
    public Image itemIcon;                  // 퀵슬롯에 표시될 아이콘
    public Text itemCountText;             // 수량 표시 텍스트(선택)

    [Header("표시 설정")]
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);
    [SerializeField] private float selectedYOffset = 12f;

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
    private static int currentHighlightedSlotNumber = 0;

    RectTransform rectTransform;
    Vector2 baseAnchoredPosition;
    Canvas canvas;
    CanvasGroup iconCanvasGroup;
    Transform iconOriginalParent;
    Vector2 iconOriginalAnchoredPos;
    bool isDraggingIcon = false;

    [SerializeField] private Image CooltimeImage; //아이템 쿨타임 이미지
    private void Update()
    {
        // 1. 필수 컴포넌트나 데이터가 없으면 조기 리턴 (안정성 확보)
        if (CooltimeImage == null) {
            // Debug.Log("쿨타임 이미지 없음");
            return;
        }

        // 아이템이 없으면 쿨타임 이미지를 0으로 만들고 종료
        if (linkedItemData == null)
        {
            // Debug.Log("no linkedItemData");
            if (CooltimeImage.fillAmount > 0) CooltimeImage.fillAmount = 0f;
            return;
        }
        if (CoolTimeManager.Instance == null) return;
        // 3. 0으로 나누기 방지 (쿨타임이 0이거나 음수인 아이템 처리)
        float maxCoolTime = linkedItemData.getCoolTime;
        if (maxCoolTime <= 0)
        {
            Debug.Log("zero division");
            CooltimeImage.fillAmount = 0f;
            return;
        }
        // 4. 실제 쿨타임 계산
        float currentCoolTime = CoolTimeManager.Instance.GetCurrentCooltime(linkedItemData.getItemName);
        
        // 5. fillAmount 갱신 (0~1 사이 값으로 클램핑하여 안전성 확보 권장 - 선택 사항)
        CooltimeImage.fillAmount = currentCoolTime / maxCoolTime;
    }    

    private void Awake()
    {
        if (!allSlots.Contains(this))
            allSlots.Add(this);

        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        canvas = GetComponentInParent<Canvas>();

        // 처음에는 아이콘 숨김
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
            itemIcon.raycastTarget = true;
            iconCanvasGroup = itemIcon.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null)
                iconCanvasGroup = itemIcon.gameObject.AddComponent<CanvasGroup>();
            iconOriginalAnchoredPos = itemIcon.rectTransform.anchoredPosition;
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

    /// <summary>
    /// 현재 포인터가 가리키는 퀵슬롯을 찾아 반환합니다.
    /// </summary>
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

    /// <summary>
    /// 등록된 퀵슬롯 리스트에서 인덱스로 검색합니다.
    /// </summary>
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

    public static void HighlightSlotByNumber(int slotNumber)
    {
        int index = slotNumber - 1;

        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null) continue;

            bool selected = slotNumber > 0 && slot.quickIndex == index;
            slot.SetSelectedVisual(selected);
        }
    }

    public void SetSelectedVisual(bool selected)
    {
        if (rectTransform == null)
            return;

        float offsetY = selected ? selectedYOffset : 0f;
        rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, offsetY);
    }

    /// <summary>
    /// 인벤토리의 Slot에서 끌어온 아이템을 이 퀵슬롯에 등록하는 함수.
    /// Drag 끝났을 때 ItemDragHandler 에서 직접 호출한다.
    /// </summary>
    public void AssignFromSlot(Slot fromSlot)
    {
        if (fromSlot == null || fromSlot.item == null) return;

        InventoryItem fromItem = fromSlot.item;

        // 필요하다면 특정 타입의 아이템만 올리도록 타입 제한을 둘 수 있다.

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

    /// <summary>
    /// 인벤토리와 연동된 개수를 갱신해 텍스트에 반영합니다.
    /// </summary>
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

    /// <summary>
    /// 연결 정보를 모두 초기화하고 아이콘/텍스트를 숨깁니다.
    /// </summary>
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

        SetSelectedVisual(false);

        if (quickIndex == currentHighlightedSlotNumber - 1)
            HighlightSlotByNumber(0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        InventoryManager.Instance?.ClearQuickSlotData(quickIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemIcon == null || linkedItemData == null)
            return;

        isDraggingIcon = true;
        iconOriginalParent = itemIcon.transform.parent;
        iconOriginalAnchoredPos = itemIcon.rectTransform.anchoredPosition;

        if (iconCanvasGroup != null)
            iconCanvasGroup.blocksRaycasts = false;

        if (canvas != null)
            itemIcon.transform.SetParent(canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingIcon || canvas == null || itemIcon == null)
            return;

        itemIcon.rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingIcon || itemIcon == null)
            return;

        isDraggingIcon = false;

        if (iconCanvasGroup != null)
            iconCanvasGroup.blocksRaycasts = true;

        itemIcon.transform.SetParent(iconOriginalParent != null ? iconOriginalParent : transform, true);
        itemIcon.rectTransform.anchoredPosition = iconOriginalAnchoredPos;

        Vector2 pointerPos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RectTransform slotRect = transform as RectTransform;
        bool droppedOnSelf = slotRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(slotRect, pointerPos, uiCamera);

        if (!droppedOnSelf)
        {
            InventoryManager.Instance?.ClearQuickSlotData(quickIndex);
        }
    }
}


