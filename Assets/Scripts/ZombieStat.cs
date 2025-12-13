using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Extensions;

// 좀비의 유형을 정의하는 Enum
public enum ZombieType
{
    Normal,    // 일반 좀비
    HighHp,    // 체력 특화 좀비 
    HighSpeed, // 속도 특화 좀비 
    HighPower  // 공격력 특화 좀비
}

public class ZombieStat : MonoBehaviour
{
    [Header("Zombie Type")]
    public ZombieType type = ZombieType.Normal; // 좀비의 유형

    [System.Serializable]
    public class DropItemData
    {
        public GameObject itemPrefab;
        [Tooltip("0~1 사이 확률")]
        [Range(0f, 1f)]
        public float dropRate = 0.2f;
    }

    [Header("Drop Items")]
    [Tooltip("좀비가 죽을 때 드롭할 수 있는 아이템 목록 (여러 개 가능)")]
    public DropItemData[] dropItems = new DropItemData[0];

    [Header("Deprecated - 사용하지 않음 (호환성 유지용)")]
    [Tooltip("이 필드는 더 이상 사용하지 않습니다. dropItems 배열을 사용하세요.")]
    public GameObject dropItem;
    [Tooltip("이 필드는 더 이상 사용하지 않습니다. dropItems 배열의 각 항목에 dropRate를 설정하세요.")]
    public float dropRate = 0.2f;

    // 기본 스탯 설정
    [Header("Base Stats")]
    public float BaseHp = 1000f;        // 기본 체력
    public float BaseSpeedFactor = 1.1f; // 기본 속도 배수
    public float BasePower = 40f;       // 기본 공격력

    [Header("NavMesh 이동 기본 속도")]
    [Tooltip("NavMeshAgent의 기본 속도 값 (에디터에서 조정 가능)")]
    public float baseNavSpeed = 1.5f;

    // 현재 스탯
    private float currentHp;
    public float power { get; private set; }
    private float currentSpeedFactor;

    private HeroStat heroStat;
    private ZombieNavMove zombieNavMove;

    // NavMesh 관련
    private AgentOverride2d agent2d;
    private NavMeshAgent navAgent;

    [Header("공격 당할 때 사운드")]
    [SerializeField]private AudioClip hitClip;


    void Awake()
    {
        // AgentOverride2d 및 내장 NavMeshAgent 가져오기
        agent2d = GetComponent<AgentOverride2d>();
        if (agent2d != null)
        {
            navAgent = agent2d.Agent;
        }
        else
        {
            // 혹시 직접 NavMeshAgent가 붙어있는 경우 대비
            navAgent = GetComponent<NavMeshAgent>();
        }

        if (navAgent == null)
        {
            Debug.LogWarning("[ZombieStat] NavMeshAgent를 찾지 못했습니다. 이동 속도 조절이 적용되지 않습니다.", this);
        }

        zombieNavMove = GetComponent<ZombieNavMove>();
    }

    void Start()
    {
        // GameManager 없으면 기본 스탯만 적용
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ZombieStat] GameManager.Instance를 찾을 수 없습니다. 기본 스탯만 사용합니다.");

            currentHp = BaseHp;
            power = BasePower;
            currentSpeedFactor = BaseSpeedFactor;

            // NavMesh 속도 반영
            ApplySpeedToNavAgent();
            return;
        }

        heroStat = HeroStat.Instance;

        // 기본 스탯으로 초기화 
        currentHp = BaseHp;
        power = BasePower;
        currentSpeedFactor = BaseSpeedFactor;

        // 밤 페이즈 확인 및 선택적 배율 적용
        if (GameManager.Instance.CurrentPhase == Phase.Night)
        {
            // Debug.Log($"[ZombieStat] 밤 페이즈! {type} 유형에 따른 능력치 증가 적용.");

            switch (type)
            {
                case ZombieType.HighHp:
                    currentHp *= GameManager.Instance.NightHpMultiplier;
                    // Debug.Log(" - 체력 특화 증가 적용됨.");
                    break;

                case ZombieType.HighSpeed:
                    currentSpeedFactor *= GameManager.Instance.NightSpeedMultiplier;
                    // Debug.Log(" - 속도 특화 증가 적용됨.");
                    break;

                case ZombieType.HighPower:
                    power *= GameManager.Instance.NightPowerMultiplier;
                    // Debug.Log(" - 공격력 특화 증가 적용됨.");
                    break;

                case ZombieType.Normal:
                default:
                    // Debug.Log(" - 일반 좀비는 밤 특화 증가가 없습니다.");
                    break;
            }
        }

        // 최종 속도를 NavMeshAgent에 적용
        ApplySpeedToNavAgent();

        // Debug.Log($"[ZombieStat] 최종 스탯 ({type}) - HP: {currentHp}, SpeedFactor: {currentSpeedFactor}, Power: {power}");
    }

    // NavMeshAgent에 실제 속도 반영
    private void ApplySpeedToNavAgent()
    {
        if (navAgent != null)
        {
            float finalSpeed = baseNavSpeed * currentSpeedFactor;
            navAgent.speed = finalSpeed;
            Debug.Log($"[ZombieStat] 속도 적용: baseNavSpeed={baseNavSpeed}, currentSpeedFactor={currentSpeedFactor}, 최종 속도={finalSpeed}");
        }
        else
        {
            Debug.LogWarning("[ZombieStat] NavMeshAgent가 null입니다. 속도를 적용할 수 없습니다.");
        }
    }

    // 좀비 파괴
    public void DestroyZombie()
    {
        // 예전: zombieMove.CallDestroy();  // ← 이제 필요 없음(삭제)
        itemDrop();
        Destroy(gameObject);
    }

    // 체력 감소 로직
    public void takeDamage(float damage)
    {
        currentHp -= damage;
        zombieNavMove?.OnDamageTaken();
        if (currentHp <= 0)
        {
            DestroyZombie();
        }
        // 사운드 매니저와 피격 사운드 클립이 유효할 때만 재생
        if (SoundManager.Instance != null && hitClip != null)
        {
            SoundManager.Instance.PlaySFX(hitClip, 1.0f);
        }
        else if (hitClip != null && SoundManager.Instance == null)
        {
            Debug.LogWarning("[ZombieStat] SoundManager.Instance 가 null 입니다. 좀비 피격 사운드를 재생할 수 없습니다.");
        }
    }

    // 아이템 드롭
    public void itemDrop()
    {
        // 새로운 dropItems 배열을 우선 사용
        if (dropItems != null && dropItems.Length > 0)
        {
            foreach (var dropData in dropItems)
            {
                if (dropData == null || dropData.itemPrefab == null)
                    continue;

                bool isDrop = Random.Range(0f, 1f) < dropData.dropRate;
                if (isDrop)
                {
                    Instantiate(dropData.itemPrefab, transform.position, Quaternion.identity);
                }
            }
        }
        // 기존 dropItem 필드 (하위 호환성 유지)
        else if (dropItem != null)
        {
            bool isDrop = Random.Range(0f, 1f) < dropRate;
            if (isDrop)
            {
                Instantiate(dropItem, transform.position, Quaternion.identity);
            }
        }
    }
}
