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
    public int hp;
    public int speed;
    private int hunger;

    private bool isSurvival;

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
