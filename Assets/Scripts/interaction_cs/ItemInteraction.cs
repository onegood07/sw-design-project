using UnityEngine;

public class ItemInteraction : MonoBehaviour, IInteractable
{
    // 가방을 드롭하고 드롭 시점에 확률 반영해서 인벤토리에 추가
    // 아이템 리스트는 인벤토리 매니저로부터 가져옴
    // 확률 반영해서 추출된 아이템명, 갯수 반영해서 인벤토리에 추가.
    private string ItemName = "test";
    private int ItemCnt = 1;
    public void OnInteract()
    {
        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.addItem(ItemName, ItemCnt);
            Destroy(gameObject);
        }
    }
}
