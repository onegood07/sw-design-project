using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    #endregion

    public delegate void OnSlotCountChange(int val);
    public OnSlotCountChange onSlotCountChange;

    public delegate void OnChangeItem();
    public OnChangeItem onChangeItem;

    // ★ InventoryItem List
    public List<InventoryItem> items = new List<InventoryItem>();

    public int slotCnt = 20;

    public bool AddItem(Item worldItem, int addCount = 1)
    {
        if (worldItem == null) return false;

        // 이미 존재하는 아이템이면 count 증가 (null 슬롯 방지)
        int idx = items.FindIndex(i => i != null && i.itemName == worldItem.itemName);
        if (idx != -1)
        {
            items[idx].count += addCount;
            onChangeItem?.Invoke();
            return true;
        }

        // 새 슬롯에 추가
        InventoryItem newItem = new InventoryItem(worldItem, addCount);

        // 먼저 비어있는 슬롯(null)을 재사용
        int emptyIdx = items.FindIndex(i => i == null);
        if (emptyIdx != -1)
        {
            items[emptyIdx] = newItem;
        }
        else
        {
            // 공간이 모자라면 리스트 확장 (slotCnt 제한은 UI에서 처리)
            items.Add(newItem);
        }

        onChangeItem?.Invoke();
        return true;
    }

    public int ConsumeItemAt(int index, int amount = 1)
    {
        if (index < 0 || index >= items.Count) return 0;
        var item = items[index];
        if (item == null) return 0;

        item.count -= amount;
        if (item.count <= 0)
        {
            items[index] = null;
            item.count = 0;
        }

        onChangeItem?.Invoke();
        return item != null ? item.count : 0;
    }
}
