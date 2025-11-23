using System.Data;
using UnityEngine;

// ScriptableObject를 상속받아 유니티 에셋으로 만들 수 있게 함.
public abstract class ItemData : ScriptableObject
{
    // 아이템을 코드에서 식별할 때 사용할 고유 ID - 문자열
    [Header("아이템 ID - 정수 지정")]
    [SerializeField]private int ItemName;
    public int getItemName
    {
        get
        {
            return ItemName;
        }
    }

    // 아이템 상세 설명
    [Header("아이템 설명")]
    [TextArea]
    [SerializeField]private string Description;

    // 인벤토리에서 표시할 아이콘
    [Header("아이템 아이콘")]
    [SerializeField]private Sprite Icon;
    public Sprite getItemIcon
    {
        get
        {
            return Icon;
        }
    }


    // 최대 소지 가능 갯수 (예: 물약 99개)
    [Header("아이템 최대 소지 갯수")]
    [SerializeField]private int MaxStackCount;
    public int getMaxStackCount
    {
        get
        {
            return MaxStackCount;
        }
    }
    
    // 아이템 사용 가능 여부
    [Header("아이템 사용 가능 여부")]
    [SerializeField]private bool isAvailable;
    public bool getIsAvailable
    {
        get
        {
            return isAvailable;
        }
    }

    // 아이템 사용시 쿨타임
    [Header("아이템 사용시 쿨타임")]
    [SerializeField]private float coolTime;
    public float getCoolTime
    {
        get
        {
            return coolTime;
        }
    }
}
