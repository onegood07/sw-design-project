using UnityEngine;

[CreateAssetMenu(fileName = "NewMedicine", menuName = "ItemData/MedicineData")]
public class MedicineData : ItemData, IUsable
{
    // 힐량, 최대 스탯 수치
    [SerializeField]private float healAmount;
    [SerializeField]private float maxAmount;

    // heroStat 에 사용된 변수명 그대로 입력.
    [SerializeField]private string whichStat;
    private HeroStat heroStat;
    // 현재 스탯 수치
    private float currentStat;

    // 인자로 받지 말고 GameObject.FindWithTag("hero") 로 변경 고민중
    // monobehavior 상속 불가로 불가능 -> 함수 호출 시 인수로 게임오브젝트 전달 고민중
    /*
    - Use 
    - 인자 : Hero 위치, 시야방향
    - 반환 값 : 없음
    */
    public void Use(Transform HeroTransform, Vector2 viewDirection)
    {
        heroStat = HeroTransform.GetComponent<HeroStat>();

        if (whichStat == "hp")
        {
            currentStat = heroStat.hp;
            maxAmount = heroStat.maxHp;
            if (currentStat == maxAmount)
            {
                Debug.Log("한계치 초과");
                return;
            }
            heroStat.hp += healAmount;
            Debug.Log("hp회복");
        }
        else if (whichStat == "hunger")
        {
            currentStat = heroStat.hunger;
            maxAmount = heroStat.maxHunger;
            if (currentStat == maxAmount)
            {
                Debug.Log("한계치 초과");
                return;
            }
            heroStat.hunger += healAmount;
            Debug.Log("포만감 회복");
        }

    }
}