using UnityEngine;

[System.Serializable]
/// <summary>
/// 인벤토리 슬롯이 참조하는 단일 아이템 스택(이름, 아이콘, 데이터 포함)입니다.
/// </summary>
public class InventoryItem
{
    public ItemView itemType;
    public string itemName;
    public Sprite itemImage;
    public int count;
    public ItemData itemData;

    /// <summary>
    /// 필드 아이템 정보를 복사해 인벤토리 표현용 데이터로 변환합니다.
    /// </summary>
    public InventoryItem(Item src, int count)
    {
        this.itemType = src.itemType;
        this.itemName = src.itemName;
        this.itemImage = src.itemImage;
        this.count = count;
        this.itemData = src.itemDataAsset;
    }
}
