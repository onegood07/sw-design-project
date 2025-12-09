using UnityEngine;

/// <summary>
/// 필드에 떨어진 아이템과 개수를 보관하고 플레이어가 주웠을 때 제거를 담당합니다.
/// </summary>
public class FieldItems : MonoBehaviour
{
    private Item item;
    public int count = 1;   // 아이템 개수

    private void Awake()
    {
        // 아이템 데이터 에셋(ScriptableObject)이 아닌, 
        // 동일 GameObject에 붙어있는 Item 컴포넌트를 참조하는 방식으로 동작하도록 가정한다.
        if (item == null)
            item = GetComponent<Item>(); 
            
        // 주의: 이 경우 Item이 데이터 에셋이 아닌 컴포넌트이며, 
        // SpawnManager에서 Item 데이터 에셋을 SetItem으로 설정했다면 이 로직과 충돌할 수 있다.
        // 현재는 SetItem 호출로 설정된 값을 우선 사용한다.
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
        // SpawnManager가 유효한지 확인
        if (SpawnManager.Instance != null)
        {
            // 파괴 전에 SpawnManager에 이 오브젝트의 영구 데이터를 제거하도록 요청
            SpawnManager.Instance.RemovePersistentItem(this.gameObject);
        }
        else
        {
            Debug.LogWarning("[FieldItems] SpawnManager 인스턴스를 찾을 수 없습니다. 영구 데이터 제거 요청 실패.");
        }
        
        // 3. 씬에서 아이템 오브젝트를 파괴합니다.
        Destroy(gameObject);
    }
}