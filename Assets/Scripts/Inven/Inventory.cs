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

        // 이미 존재하는 아이템이면 count 증가
        int idx = items.FindIndex(i => i.itemName == worldItem.itemName);
        if (idx != -1)
        {
            items[idx].count += addCount;
            onChangeItem?.Invoke();
            return true;
        }

        // 새 슬롯에 추가
        InventoryItem newItem = new InventoryItem(worldItem, addCount);
        items.Add(newItem);

        onChangeItem?.Invoke();
        return true;
    }
}
