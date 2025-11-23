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
                Debug.LogError("[ItemDragHandler] 부모에 Slot 컴포넌트가 없습니다.", this);
            }
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ItemDragHandler] 상위에 Canvas가 없습니다.", this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slot == null || canvas == null) return;
        if (slot.item == null) return; // ★ InventoryItem 기준

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

        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPos;

        currentlyDragging = null;
    }
}
