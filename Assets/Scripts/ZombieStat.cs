using UnityEngine;

public class ZombieStat : MonoBehaviour
{
    int hp = 1000;
    int speed;
    int power = 1000;
    private HeroStat heroStat;
  
    void Start()
    {
        heroStat = HeroStat.Instance;
        if (heroStat != null) speed = (int)(heroStat.speed * 1.1f);
        else speed = 1000;
    }

    void Update()
    {
        if (hp <= 0) DestroyZombie();
    }
    void DestroyZombie() // public 으로 변경하였음
    {
        Destroy(gameObject);
    }
    public void takeDamage(int damage) // zombie hp 데이터 타입 변경 필요성 있음
    {
        hp -= damage;
    }
}
