
using UnityEngine;
using UnityEngine.InputSystem;

// 현재 퀵슬롯 아이템 사용만 구현된 상태. 
// 인벤토리에서 직접 사용은 UI가 나와야 할 수 있을 것 같음
public class HeroItemUse : MonoBehaviour
{

    public HeroMoveControl HeroMoveControl;
    private Vector2 viewDirection;
    void Start()
    {
        // 이중으로 받아옴. 인스펙터에 지정하지 않아도 됨.
        HeroMoveControl = GetComponent<HeroMoveControl>();
        if (HeroMoveControl == null)
        {
            Debug.Log("heroMoveControl 참조 불가");
        }
    }
    public void OnQuickSlot1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InventoryManager.Instance.useQuickSlotItem(1);
        }
    }
    public void OnQuickSlot2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InventoryManager.Instance.useQuickSlotItem(2);
        }
    }
    public void OnQuickSlot3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InventoryManager.Instance.useQuickSlotItem(3);
        }
    }
    public void OnQuickSlot4(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InventoryManager.Instance.useQuickSlotItem(4);
        }
    }

}