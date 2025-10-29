using System.Threading.Tasks.Dataflow;
using UnityEngine;

public class ZombieStat : MonoBehaviour
{
    int hp = 1000;
    int speed = 1000;
    int power = 1000;
  
    void Start()
    {
        
    }

    void Update()
    {
        if (hp <= 0) DestroyZombie();
    }
    void DestroyZombie()
    {
        DestroyZombie(gameObject);
    }
}
