using UnityEngine;

/// <summary>
/// 필드에 떨어진 아이템과 개수를 보관하고 플레이어가 주웠을 때 제거를 담당합니다.
/// </summary>
public class FieldItems : MonoBehaviour
{
    private Item item;
    public int count = 1;   // ★ 아이템 개수 추가

    private void Awake()
    {
        if (item == null)
            item = GetComponent<Item>();   // ★ 자동 연결
    }
    /// <summary>
    /// 드랍 정보를 외부에서 지정할 때 사용하며 아이템과 개수를 동기화합니다.
    /// </summary>
    public void SetItem(Item newItem, int newCount = 1)
    {
        item = newItem;
        count = newCount;
    }

    /// <summary>
    /// 현재 필드 아이템 데이터를 반환합니다.
    /// </summary>
    public Item GetItem()
    {
        return item;
    }

    /// <summary>
    /// 쌓여 있는 아이템 개수를 반환합니다.
    /// </summary>
    public int GetCount()
    {
        return count;
    }

    /// <summary>
    /// 필드 오브젝트를 제거해 재사용되지 않도록 합니다.
    /// </summary>
    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}
