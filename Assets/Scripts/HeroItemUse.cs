
using UnityEngine;
using UnityEngine.InputSystem;

// 인벤토리 매니저 기능 과다로 인한 기능분리
// 아이템 사용 시, 아이템 내용은 매니저로부터 참조하여 해당 스크립트에서 사용한다.
public class HeroItemUse : MonoBehaviour
{

    private HeroMoveControl HeroMoveControl;
    
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