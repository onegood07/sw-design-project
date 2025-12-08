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
    [Header("납입아이템의 경우 점수")]
    [SerializeField]private int Score;
    public int getScore
    {
        get
        {
            return Score;
        }
    }


    // 아이템 상세 설명
    [Header("아이템 설명")]
    [TextArea]
    [SerializeField]private string Description;

    [Header("아이템 아이콘 - 인벤토리에서 구현해야 함. 할당은 받아 둠")]
    [SerializeField]private Sprite Icon;
    public Sprite getItemIcon
    {
        get
        {
            return Icon;
        }
    }


    // 최대 소지 가능 갯수 (예: 물약 99개)
    [Header("아이템 최대 소지 갯수 - 인벤토리에서 구현해야 함")]
    [SerializeField]private int MaxStackCount;
    public int getMaxStackCount
    {
        get
        {
            return MaxStackCount;
        }
    }
    
    // 아이템 사용 가능 여부
    [Header("아이템 사용 가능 여부 - IUsable 상속으로 구현(인스펙터에서는 참고만 한다)")]
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
    [SerializeField]private float originCoolTime;
    public float getCoolTime
    {
        get
        {
            return originCoolTime;
        }
    }
    [Header("아이템 사용시 사운드")]
    [SerializeField]private AudioClip clip;
    public AudioClip getClip
    {
        get
        {
            return clip;
        }
    }
}
