using UnityEngine;

// ScriptableObject를 상속받아 유니티 에셋으로 만들 수 있게 합니다.
public abstract class ItemData : ScriptableObject
{
    [Header("Base Item Data")]
    // 아이템을 코드에서 식별할 때 사용할 고유 ID
    public string ID;

    // 유저에게 표시될 이름
    public string ItemName;

    // 아이템 상세 설명
    [TextArea]
    public string Description;

    // 인벤토리에서 표시할 아이콘
    public Sprite Icon;

    // 최대 중첩 가능 갯수 (예: 물약 99개)
    public int MaxStackCount = 1;
    // 아이템 사용 가능 여부
    public bool isAvailable;
}
