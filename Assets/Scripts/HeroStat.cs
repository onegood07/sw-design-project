using UnityEngine;

public class HeroStat : MonoBehaviour 
{
    
    public static HeroStat Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    public float hp;
    public float maxHp = 1000f;
    public float hunger;
    public float maxHunger = 1000f;
    public float speed;

    private bool isSurvival;

    void SpeedControl()
    {
        if (hunger <= 100) speed = 100;
    }
    public void decreaseHp(float zombiePower)
    {
        hp -= zombiePower;
        if (hp <= 0) isSurvival = false; // 사망 시 로직 구현 필요
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
