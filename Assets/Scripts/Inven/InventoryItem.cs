using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemView itemType;
    public string itemName;
    public Sprite itemImage;
    public int count;

    public InventoryItem(Item src, int count)
    {
        this.itemType = src.itemType;
        this.itemName = src.itemName;
        this.itemImage = src.itemImage;
        this.count = count;
    }
}
