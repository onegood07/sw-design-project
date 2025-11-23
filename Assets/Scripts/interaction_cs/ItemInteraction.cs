using UnityEngine;

public class ItemInteraction : MonoBehaviour, IInteractable
{
    private FieldItems fieldItems;

    private void Awake()
    {
        fieldItems = GetComponent<FieldItems>();
    }

    public void OnInteract()
    {
        if (fieldItems == null) return;

        Item itemData = fieldItems.GetItem();
        int itemCnt   = fieldItems.GetCount();

        // 슬롯 인벤토리로 넣기
        if (Inventory.instance != null && itemData != null)
        {
            for (int i = 0; i < itemCnt; i++)
            {
                Inventory.instance.AddItem(itemData);
            }
        }

        fieldItems.DestroyItem();
    }
}