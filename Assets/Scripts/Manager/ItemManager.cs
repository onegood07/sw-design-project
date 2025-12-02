using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Heal,
    Weapon,
    Lantern,
    Quest,
    Material,
    Submit
}

public class ItemManager : MonoBehaviour
{
    private Dictionary<ItemType, List<GameObject>> itemDictionary = new Dictionary<ItemType, List<GameObject>>()
    {
        { ItemType.Heal, new List<GameObject>() },
        { ItemType.Weapon, new List<GameObject>() },
        { ItemType.Lantern, new List<GameObject>() }
    };
    public static ItemManager instance;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            // ⭐ 중요: 씬이 바뀌어도 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }

    public void ClearItems()
    {
        foreach (var list in itemDictionary.Values) list.Clear();
    }

    public void RegisterSpawnedItem(GameObject item, ItemType type)
    {
        // 1. [핵심 수정] 딕셔너리에 해당 타입의 키가 아예 없으면 -> 리스트를 새로 만들어서 넣어준다.
        if (!itemDictionary.ContainsKey(type))
            itemDictionary[type] = new List<GameObject>();
  
        if (!itemDictionary[type].Contains(item))
            itemDictionary[type].Add(item);
    }

    public void SubmitItem(GameObject item, ItemType type)
    {
        if (itemDictionary[type].Contains(item))
        {
            itemDictionary[type].Remove(item);
            Debug.Log($"[{type}] 아이템 납입 완료: {item.name}");
        }
    }

    public int GetRemainingItemCount(ItemType type) => itemDictionary[type].Count;
    public int GetTotalRemainingItemCount()
    {
        int total = 0;
        foreach (var list in itemDictionary.Values) total += list.Count;
        return total;
    }
}
