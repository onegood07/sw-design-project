using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
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
        if (slot.item == null) // ★ InventoryItem 기준
        {
            Debug.LogWarning("OnBeginDrag: slot.item이 null이어서 드래그를 시작할 수 없습니다. (슬롯이 비어있음) 객체: " + gameObject.name);
            return;
        }

        currentlyDragging = this;

        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this || canvas == null) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

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
            QuickSlot quickSlot = QuickSlot.FindSlotUnderPointer(pointerPos, uiCamera);
            if (quickSlot != null)
            {
                quickSlot.AssignFromSlot(slot);
            }
        }

        // 아이콘 원래 자리로 복귀 (실제 데이터는 Slot/EquipSlot/QuickSlot 이 관리)
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPos;

        currentlyDragging = null;
    }
}
