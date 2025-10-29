using UnityEngine;

public class HeroStat : MonoBehaviour
{
    int hp;
    int speed;
    int hunger;

    bool isSurvival;

    void SpeedControl()
    {
        if (hunger <= 100) speed = 100;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hp = 1000;
        speed = 1000;
        hunger = 1000;
        isSurvival = true;
    }

    // Update is called once per frame
    void Update()
    {
        SpeedControl();
    }
}
