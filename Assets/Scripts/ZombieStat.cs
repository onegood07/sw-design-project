using UnityEngine;

// 좀비의 유형을 정의하는 Enum
public enum ZombieType { 
    Normal, // 일반 좀비 (밤에 특화 증가 없음)
    HighHp, // 체력 특화 좀비 (Tank)
    HighSpeed, // 속도 특화 좀비 (Runner)
    HighPower // 공격력 특화 좀비 (Bruiser)
}

public class ZombieStat : MonoBehaviour
{
    // === 좀비 유형 설정 (Unity 에디터에서 설정) ===
    [Header("Zombie Type")]
    public ZombieType type = ZombieType.Normal; // 이 좀비의 유형을 설정합니다.

    // === 기본 스탯 설정 (Unity 에디터에서 설정) ===
    [Header("Base Stats")]
    public float BaseHp = 1000f; // 기본 체력
    public float BaseSpeedFactor = 1.1f; // 기본 속도 배수
    public float BasePower = 40f; // 기본 공격력

    // === 현재 스탯 (게임 중 사용) ===
    private float currentHp;
    
    // ★ ZombieMove.cs의 DoAttack()에서 'GetComponent<ZombieStat>().power'로 참조하는 
    // 오류를 해결하기 위해 public 프로퍼티로 정의했습니다.
    public float power { get; private set; } 
    
    private float currentSpeedFactor;
    
    private HeroStat heroStat;
    private ZombieMove zombieMove; // ZombieMove 컴포넌트 참조

    void Awake()
    {
        // ZombieMove 컴포넌트 참조
        zombieMove = GetComponent<ZombieMove>();
    }

    void Start()
    {
        // GameManager 인스턴스가 존재하는지 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ZombieStat] GameManager.Instance를 찾을 수 없습니다. 게임 상태가 초기화되지 않았습니다.");
            
            // GameManager가 없으면 기본 스탯만 적용하고 종료
            currentHp = BaseHp;
            power = BasePower;
            currentSpeedFactor = BaseSpeedFactor;

            // ZombieMove의 public 'speed' 필드에 기본값 반영
            if (zombieMove != null) zombieMove.speed = currentSpeedFactor;

            return;
        }

        heroStat = HeroStat.Instance; 

        // 1. 기본 스탯으로 초기화 
        currentHp = BaseHp;
        power = BasePower; // public 'power' 필드 초기화
        currentSpeedFactor = BaseSpeedFactor; 

        // 2. 밤 페이즈 확인 및 **선택적** 배율 적용 (핵심 로직)
       if (GameManager.Instance.CurrentPhase == Phase.Night)
        {
            Debug.Log($"[ZombieStat] 밤 페이즈! {type} 유형에 따른 능력치 증가 적용.");
            
            switch (type)
            {
                case ZombieType.HighHp:
                    // 체력 특화
                    currentHp *= GameManager.Instance.NightHpMultiplier;
                    Debug.Log(" - 체력 특화 증가 적용됨.");
                    break;

                case ZombieType.HighSpeed:
                    // 속도 특화: GameManager에 설정된 속도 배율을 현재 배율에 곱함
                    currentSpeedFactor *= GameManager.Instance.NightSpeedMultiplier;
                    Debug.Log(" - 속도 특화 증가 적용됨.");
                    break;

                case ZombieType.HighPower:
                    // 공격력 특화
                    power *= GameManager.Instance.NightPowerMultiplier; // public 'power' 필드에 적용
                    Debug.Log(" - 공격력 특화 증가 적용됨.");
                    break;
                
                case ZombieType.Normal: // 일반 좀비는 밤에 능력치 변화 없음
                default:
                    Debug.Log(" - 일반 좀비는 밤 특화 증가가 없습니다.");
                    break;
            }
        }

        // 최종 계산된 속도 배율을 ZombieMove 스크립트의 public 'speed' 필드에 반영합니다.
        if (zombieMove != null)
        {
            zombieMove.speed = currentSpeedFactor; 
        }
        
        Debug.Log($"[ZombieStat] 최종 스탯 ({type}) - HP: {currentHp}, SpeedFactor: {currentSpeedFactor}, Power: {power}");
    }

    // 외부에서 현재 체력 값을 읽을 수 있도록 프로퍼티 추가 (옵션)
    public float CurrentHp => currentHp;

    public void DestroyZombie() 
    {
        Destroy(gameObject);
    }
    
    // 체력 감소 로직
    public void takeDamage(float damage)
    {
        currentHp -= damage;
        if(currentHp <= 0) DestroyZombie();
    }
}