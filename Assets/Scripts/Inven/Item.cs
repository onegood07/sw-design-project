using UnityEngine;

/// <summary>
/// UI와 장비 슬롯에서 구분하기 위한 아이템 카테고리입니다.
/// </summary>
public enum ItemView
{
    Heal,
    Weapon,
    Lantern,
    Unused
}

[System.Serializable]
/// <summary>
/// 필드 오브젝트가 보유하는 기본 아이템 정보 및 사용 로직의 뼈대입니다.
/// </summary>
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
