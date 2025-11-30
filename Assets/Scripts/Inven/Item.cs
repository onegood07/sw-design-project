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

    [Header("퀵슬롯/사용 데이터 (선택)")]
    public ItemData itemDataAsset;

    public virtual bool Use()
    {
        return false;
    }
}
