using UnityEngine;

public class ItemInteraction : MonoBehaviour, IInteractable
{
    // 아이템 리스트는 인벤토리 매니저로부터 가져옴
    // 아이템 스크립트(scriptable object), 갯수 반영해서 인벤토리에 추가.

    // 추후 변수 수정이 있을 예정.
    private string ItemName = "test";
    private int ItemCnt = 1;
    // 해당 OnInteract 에 작성한 코드 내용이 상호작용 시 수행됨.
    public void OnInteract()
    {
        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.addItem(ItemName, ItemCnt);
            Destroy(gameObject);
        }
    }
}
