using UnityEngine;

public class FieldItems : MonoBehaviour
{
    private Item item;
    public int count = 1;   // ★ 아이템 개수 추가

    private void Awake()
    {
        if (item == null)
            item = GetComponent<Item>();   // ★ 자동 연결
    }
    public void SetItem(Item newItem, int newCount = 1)
    {
        item = newItem;
        count = newCount;
    }

    public Item GetItem()
    {
        return item;
    }

    public int GetCount()
    {
        return count;
    }

    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}
