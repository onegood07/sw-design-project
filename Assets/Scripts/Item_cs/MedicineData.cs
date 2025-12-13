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
    - 인자 : Hero 위치(현재는 사용하지 않지만 IUsable 규격 유지용), 시야방향
    - 반환 값 : 없음
    */
    public void Use(Transform HeroTransform, Vector2 viewDirection)
    {
        // ✅ 항상 싱글톤 HeroStat을 기준으로 동작하도록 수정
        //    (잘못된 Transform이 넘어와도 HP 회복이 제대로 되도록 함)
        heroStat = HeroStat.Instance;
        if (heroStat == null)
        {
            Debug.LogWarning("[MedicineData] HeroStat.Instance 를 찾을 수 없어 회복 아이템을 사용할 수 없습니다.");
            return;
        }

        // hp 회복 아이템 사용 시.
        if (whichStat.Contains("hp"))
        {
            currentStat = heroStat.hp;
            maxAmount = heroStat.maxHp;
            // HP가 이미 최대치면 HP만 건너뛰고, 다른 스탯(허기 등)은 계속 처리한다.
            if (currentStat >= maxAmount)
            {
                Debug.Log("HP 한계치 초과 - HP 회복은 생략, 다른 스탯은 계속 처리");
            }
            else
            {
                // 현재 스탯에 회복량을 더했을 때 최고치를 넘는 경우 최대치로 스탯을 조정
                float newHp = currentStat + healAmount[0];
                heroStat.hp = Mathf.Min(newHp, maxAmount);
                Debug.Log("hp 회복");
            }
        }
        // 허기 회복 아이템 사용 시.
        if (whichStat.Contains("hunger"))
        {
            currentStat = heroStat.hunger;
            maxAmount = heroStat.maxHunger;
            // 허기가 이미 최대치면 허기 회복만 생략하고, 다른 스탯은 계속 처리
            if (currentStat>=maxAmount)
            {
                Debug.Log("허기 한계치 초과 - 허기 회복은 생략");
            }
            else
            {
                // 현재 스탯에 회복량을 더했을 때 최고치를 넘는 경우 최대치로 스탯을 조정
                float newHunger = currentStat + healAmount[1];
                heroStat.hunger = Mathf.Min(newHunger, maxAmount);
                Debug.Log("포만감 회복");
            }
        }
        if (whichStat.Contains("speed"))
        {
            // 이동속도 증가 코루틴 적용.
            heroStat.ActiveHeroSpeedBoost(buffDuration,healAmount[2]);
        }
    }
}