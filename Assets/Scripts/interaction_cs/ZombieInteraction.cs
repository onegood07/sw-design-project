using UnityEngine;

// 좀비 상호작용을 수행하는 스크립트 (Hero -> Zombie) 에 해당하는 과정임.
public class ZombieInteraction : MonoBehaviour, IInteractable
{
    // 상호작용한 타겟 좀비의 상호작용을 동작시킴
    private ZombieStat zombieStat;
    private HeroStat heroStat;
    public void OnInteract()
    {
        // 테스트를 위해 임의로 작성한 데미지임. heroStat을 참고하여 데미지 가하도록 변경 필요
        zombieStat.takeDamage(400); 
        Debug.Log("attack!");
    }
    private void Start()
    {
        zombieStat = GetComponent<ZombieStat>();
        heroStat = HeroStat.Instance;
        if(heroStat == null)Debug.Log("hero_stat reference error");
        if (zombieStat == null) Debug.Log("zombie_stat reference error");
    }
}
