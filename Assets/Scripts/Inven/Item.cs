using UnityEngine;

public enum ItemView
{
    Heal,
    Weapon,
    Lantern,
    Unused
}

[System.Serializable]
public class Item : MonoBehaviour
{
    public ItemView itemType;
    public string itemName;
    public Sprite itemImage;

    public virtual bool Use()
    {
        return false;
    }
}
