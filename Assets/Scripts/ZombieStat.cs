using UnityEngine;

public class ZombieStat : MonoBehaviour
{
    int hp = 1000;
    int speed;
    int power = 1000;
    public Transform player;
    private HeroStat heroStat;
  
    void Start()
    {
        heroStat = player.GetComponent<HeroStat>();
        if (heroStat != null) speed = (int)(heroStat.speed * 1.1f);
        else speed = 1000;
    }

    void Update()
    {
        if (hp <= 0) DestroyZombie();
    }
    void DestroyZombie()
    {
        Destroy(gameObject);
    }
}
