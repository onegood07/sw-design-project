using UnityEngine;

[CreateAssetMenu(fileName = "NewMedicine", menuName = "ItemData/MedicineData")]
public class MedicineData : ItemData, IUsable
{
    // 힐량, 최대 스탯 수치
    public float healAmount;
    private float maxAmount;
    // heroStat 에 사용된 변수명 그대로 입력.
    public string whichStat;
    private HeroStat heroStat;
    // 현재 스탯 수치
    private float currentStat;


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