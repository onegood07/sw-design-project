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
    private Dictionary<ItemType, List<GameObject>> itemDictionary = new Dictionary<ItemType, List<GameObject>>()
    {
        { ItemType.Heal, new List<GameObject>() },
        { ItemType.Weapon, new List<GameObject>() },
        { ItemType.Lantern, new List<GameObject>() }
    };

    public void ClearItems()
    {
        foreach (var list in itemDictionary.Values) list.Clear();
    }

    public void RegisterSpawnedItem(GameObject item, ItemType type)
    {
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
