using UnityEngine;

public class ItemInteraction : MonoBehaviour, IInteractable
{
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
