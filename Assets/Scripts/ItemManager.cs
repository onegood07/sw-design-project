using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Heal,
    Weapon,
    Lantern
}

public class ItemManager : MonoBehaviour
{
    // 아이템 타입별 등록
    private Dictionary<ItemType, List<GameObject>> itemDictionary = new Dictionary<ItemType, List<GameObject>>()
    {
        { ItemType.Heal, new List<GameObject>() },
        { ItemType.Weapon, new List<GameObject>() },
        { ItemType.Lantern, new List<GameObject>() }
    };

    // 납입 아이템 리스트 초기화
    public void ClearItems()
    {
       foreach (var list in itemDictionary.Values) list.Clear();
    }

    // SpawnManager가 새로 스폰한 아이템 등록
    public void RegisterSpawnedItem(GameObject item, ItemType type)
    {
        if (!itemDictionary[type].Contains(item))
            itemDictionary[type].Add(item);
    }

    // 특정 타입 아이템 납입
    public void SubmitItem(GameObject item, ItemType type)
    {
        if (itemDictionary[type].Contains(item))
        {
            itemDictionary[type].Remove(item);
            Debug.Log($"[{type}] 아이템 납입 완료: {item.name}");
        }
    }

    // 특정 타입 남은 아이템 수
    public int GetRemainingItemCount(ItemType type)
    {
        return itemDictionary[type].Count;
    }

    // 전체 남은 아이템 수
    public int GetTotalRemainingItemCount()
    {
        int total = 0;
        foreach (var list in itemDictionary.Values)
            total += list.Count;
        return total;
    }

}
