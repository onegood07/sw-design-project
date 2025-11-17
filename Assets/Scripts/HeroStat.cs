using UnityEngine;
using System.Collections; 

public class HeroStat : MonoBehaviour 
{
    public static HeroStat Instance { get; private set; }

    // 생명력
    public float hp;
    public float maxHp = 1000f;

    // 허기
    public float hunger;
    public float maxHunger = 1000f;

    // 이동속도
    public float speed;
    public bool isSurvival;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

     void Start()
    {
        hp = 1000;
        speed = 1000;
        hunger = 1000;
        isSurvival = true;

        // 일정 시간마다 허기 감소 로직
        StartCoroutine(HungerDecreaseCoroutine());
    }

    void SpeedControl()
    {
        if (hunger <= 100) speed = 100;
    }

    // 생명력 감소 로직
    public void decreaseHp(float zombiePower)
    {
        // 좀비의 공격력(Power)만큼 생명력 감소
        hp -= zombiePower;

        if (hp <= 0 && isSurvival) 
        {
            isSurvival = false;
            Debug.Log("플레이어 사망!");
            // 플레이어 사망 시에 GameManager의 플레이어 사망 로직 불러오기
            GameManager.Instance?.PlayerDied();
        }
    }

    void Update()
    {
        SpeedControl();
    }

    // 일정 시간마다 허기 감소 코루틴
    IEnumerator HungerDecreaseCoroutine()
    {
        while (isSurvival)
        {
            // 1초마다 허기가 10씩 감소
            yield return new WaitForSeconds(1f);
            hunger -= 10f;

            // 허기가 음수가 되지 않도록 보정
            if (hunger < 0) hunger = 0;

            // TODO: 허기가 0이 되면 플레이어 피해 처리 (추후 구현)
            if (hunger <= 0)
            {
                // decreaseHp(10f); 
            }
        }
    }
}

