using UnityEngine;

public class ZombieStat : MonoBehaviour
{
    float hp = 1000;
    float speed;
    float power = 1000;
    private HeroStat heroStat;
  
    void Start()
    {
        heroStat = HeroStat.Instance;
        if (heroStat != null) speed = (int)(heroStat.speed * 1.1f);
        else speed = 1000;
    }
    public void DestroyZombie() // public 으로 변경하였음
    {
        Destroy(gameObject);
    }
    public void takeDamage(float damage) // zombie hp 데이터 타입 변경 필요성 있음
    {
        hp -= damage;
        if(hp <= 0)DestroyZombie();
    }

}
