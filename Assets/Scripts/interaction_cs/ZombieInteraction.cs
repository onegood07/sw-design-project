using System.Runtime.CompilerServices;
using UnityEngine;

public class ZombieInteraction : MonoBehaviour, IInteractable
{
    private ZombieStat zombieStat;
    private HeroStat heroStat;
    public void OnInteract()
    {
        // 필수 변경
        // 임의 설정 heroStat.power를 추가해야 할듯.
        zombieStat.takeDamage(200); 
        Debug.Log("attack!");
    }
    private void Start()
    {
        zombieStat = GetComponent<ZombieStat>();
        heroStat = zombieStat.player.GetComponent<HeroStat>();
        if(heroStat == null)Debug.Log("hero_stat reference error");
        if (zombieStat == null) Debug.Log("zombie_stat reference error");
    }
}
