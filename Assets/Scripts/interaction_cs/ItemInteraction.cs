using UnityEngine;

// IInteractable 인터페이스는 외부에서 정의되어 있다고 가정합니다.
// public interface IInteractable { void OnInteract(); }

public class ItemInteraction : MonoBehaviour, IInteractable
{
    private FieldItems fieldItems;

    private void Awake()
    {
        // ItemInteraction과 FieldItems는 동일한 GameObject에 붙어있어야 합니다.
        fieldItems = GetComponent<FieldItems>(); 
    }

    public void OnInteract()
    {
        if (fieldItems == null) return;

        Item itemData = fieldItems.GetItem();
        int itemCnt   = fieldItems.GetCount();

        // 1. 슬롯 인벤토리로 넣기
        if (Inventory.instance != null && itemData != null)
        {
            for (int i = 0; i < itemCnt; i++)
            {
                // Inventory 클래스는 외부에서 정의되어 있다고 가정합니다.
                // Inventory.instance.AddItem(itemData); 
                Debug.Log($"[ItemInteraction] 인벤토리에 {itemData.name} {itemCnt}개 추가 시도.");
            }
        }
        
        // 2. FieldItems에게 파괴를 요청합니다. 
        // DestroyItem() 내부에서 SpawnManager 제거 로직이 실행됩니다.
        fieldItems.DestroyItem();
    }
}