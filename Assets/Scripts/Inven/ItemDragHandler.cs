using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
/// <summary>
/// 인벤토리 슬롯 아이콘을 드래그하여 다른 슬롯/장비/퀵슬롯으로 옮기는 입력 처리기입니다.
/// </summary>
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static ItemDragHandler currentlyDragging;

    public Slot slot;  // 이 아이콘이 속한 슬롯

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (slot == null)
        {
            slot = GetComponentInParent<Slot>();
            if (slot == null)
            {
                // 인벤토리 슬롯이 아닌 곳에 잘못 붙어 있는 경우를 대비한 방어 코드
                // 에러를 계속 뿜지 않고, 이 컴포넌트를 비활성화해서 드래그가 동작하지 않게만 한다.
                Debug.LogWarning("[ItemDragHandler.Awake] 부모에 Slot 컴포넌트가 없어 ItemDragHandler를 비활성화합니다. 객체: " + name, this);
                enabled = false;
                return;
            } else {
                Debug.Log("[ItemDragHandler.Awake] Slot 컴포넌트 발견: " + slot.name, this);
            }
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ItemDragHandler.Awake] 상위에 Canvas가 없습니다.", this);
        } else {
            Debug.Log("[ItemDragHandler.Awake] Canvas 컴포넌트 발견: " + canvas.name, this);
        }
    }

    /// <summary>
    /// 아이콘이 다시 활성화될 때마다 레이캐스트를 복구해서 드래그가 막히지 않도록 한다.
    /// (TrashZone에서 버리는 과정 중 OnEndDrag가 호출되지 않으면 blocksRaycasts 가 false로 남을 수 있음)
    /// </summary>
    void OnEnable()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (currentlyDragging == this)
            currentlyDragging = null;
    }

    /// <summary>
    /// 드래그를 시작하며 캔버스로 이동시키고 레이캐스트를 비활성화합니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag 호출됨. 드래그하려는 객체: " + gameObject.name);
        if (slot == null)
        {
            Debug.LogWarning("OnBeginDrag: slot이 null이어서 드래그를 시작할 수 없습니다. 객체: " + gameObject.name);
            return;
        }
        if (canvas == null)
        {
            Debug.LogWarning("OnBeginDrag: canvas가 null이어서 드래그를 시작할 수 없습니다. 객체: " + gameObject.name);
            return;
        }

        // 혹시 slot.item 이 인벤토리 데이터와 싱크가 안 맞는 경우를 대비해 한 번 동기화 (최신 버전 추가 로직)
        if (slot.item == null && Inventory.instance != null)
        {
            int idx = slot.slotIndex;
            if (idx >= 0 && idx < Inventory.instance.items.Count)
            {
                slot.item = Inventory.instance.items[idx];
            }
        }

        if (slot.item == null) // ★ InventoryItem 기준
        {
            Debug.LogWarning("OnBeginDrag: slot.item이 null이어서 드래그를 시작할 수 없습니다. (슬롯이 비어있음) 객체: " + gameObject.name);
            return;
        }

        if (slot.item != null)
        {
            // 'itemName'은 InventoryItem 클래스의 public 변수입니다.
            Debug.Log($"[Item Drag Start] 드래그 시작 아이템 이름: {slot.item.itemName}");
        }

        currentlyDragging = this;

        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 마우스 이동량을 따라다니도록 아이콘 위치를 업데이트합니다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this || canvas == null) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    /// <summary>
    /// 드래그 종료 시 장비/퀵슬롯 할당을 시도하고 아이콘을 되돌립니다.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this) return;

        canvasGroup.blocksRaycasts = true;

        Vector2 pointerPos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        bool handled = false;

        // 장비 슬롯 검사
        EquipSlot equipSlot = EquipSlot.FindSlotUnderPointer(pointerPos, uiCamera);
        if (equipSlot != null)
        {
            equipSlot.EquipFromSlot(slot);
            handled = true;
        }

        if (!handled)
        {
            // 슬롯에 못넣는 아이템
            if(slot.item.itemType != ItemView.Lantern)
            {
                QuickSlot quickSlot = QuickSlot.FindSlotUnderPointer(pointerPos, uiCamera);
                if (quickSlot != null)
                {
                    quickSlot.AssignFromSlot(slot);
                }
            }
        }

        // 아이콘 원래 자리로 복귀 (실제 데이터는 Slot/EquipSlot/QuickSlot 이 관리)
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPos;

        currentlyDragging = null;
    }

    /// <summary>
    /// 외부(예: TrashZone)에서 드래그 아이콘을 원래 부모/위치로 되돌리고 싶을 때 호출합니다. (최신 버전 추가 함수)
    /// </summary>
    public void RestoreToOriginal()
    {
        if (rectTransform == null)
            return;

        if (originalParent != null)
            transform.SetParent(originalParent, true);

        rectTransform.anchoredPosition = originalPos;
    }
}