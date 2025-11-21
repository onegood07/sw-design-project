using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMedicine", menuName = "ItemData/MedicineData")]
public class MedicineData : ItemData, IUsable
{
    // heroStat 에 사용된 변수명 그대로 입력.
    [Header("어떤 스탯을 회복시킬 것인지- heroStat의 변수명을 그대로 사용하여야 함")]
    [SerializeField]private string[] whichStat;
    // 힐량, 최대 스탯 수치
    [Header("회복량 - 배열 위치는 hp[0], hunger[1], speed [2]순이다")]
    [SerializeField]private float[] healAmount;
    [Header("버프 효과가 있다면 지속시간")]
    [SerializeField]private float buffDuration;
    private float maxAmount;

    private HeroStat heroStat;
    // 현재 스탯 수치
    private float currentStat;

    // 인자로 받지 말고 GameObject.FindWithTag("hero") 로 변경 고민중
    // monobehavior 상속 불가로 불가능 -> 함수 호출 시 인수로 게임오브젝트 전달 고민중
    // 코드 성능 고려하면 그게 조금 나은 것 같긴함
    /*
    - Use 
    - 인자 : Hero 위치, 시야방향
    - 반환 값 : 없음
    */
    public void Use(Transform HeroTransform, Vector2 viewDirection)
    {
        heroStat = HeroTransform.GetComponent<HeroStat>();

        if (whichStat.Contains("hp"))
        {
            currentStat = heroStat.hp;
            maxAmount = heroStat.maxHp;
            if (currentStat == maxAmount)
            {
                Debug.Log("한계치 초과");
                return;
            }
            heroStat.hp += healAmount[0];
            Debug.Log("hp회복");
        }
        if (whichStat.Contains("hunger"))
        {
            currentStat = heroStat.hunger;
            maxAmount = heroStat.maxHunger;
            if (currentStat == maxAmount)
            {
                Debug.Log("한계치 초과");
                return;
            }
            heroStat.hunger += healAmount[1];
            Debug.Log("포만감 회복");
        }
        if (whichStat.Contains("speed"))
        {
            heroStat.ActiveHeroSpeedBoost(buffDuration,healAmount[2]);
        }
    }
}