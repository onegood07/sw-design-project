using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMove : MonoBehaviour
{
    [Header("이동 / 탐지")]
    [SerializeField] float speed = 3f;      // NavMeshAgent 이동 속도
    [SerializeField] float chaseRange = 5f; // 이 거리 안에 들어오면 플레이어 추적
    [SerializeField] float attackRange = 0.8f; // 공격 사거리
    [SerializeField] float attackDelay = 0.5f; // ★ 공격 딜레이(초) = 0.5f 고정

    [Header("배회 설정")]
    [SerializeField] float wanderRadius = 2f;     // 배회시 이동 가능한 반경
    [SerializeField] float wanderInterval = 2f;   // 배회 목적지 갱신 간격

    Animator animator;
    NavMeshAgent agent;
    Transform heroTr;
    HeroStat heroStat;
    ZombieStat zombieStat;

    bool isLive = true;
    bool inAttackRange = false;      // 공격 범위 안에 처음 들어온 순간 체크용
    float attackTimer = 0f;          // 공격 쿨타임
    float wanderTimer = 0f;          // 배회 목적지 갱신 타이머
    Vector2Int lastMoveDir = Vector2Int.down; // 마지막 이동 방향(애니메이션용)

    void Awake()
    {
        animator   = GetComponent<Animator>();
        agent      = GetComponent<NavMeshAgent>();
        zombieStat = GetComponent<ZombieStat>();

        // Hero 찾기
        var heroGo = GameObject.Find("Hero");
        if (heroGo != null)
        {
            heroTr   = heroGo.transform;
            heroStat = heroGo.GetComponent<HeroStat>();
        }

        // NavMeshAgent 2D 세팅
        if (agent != null)
        {
            agent.updateRotation = false; // 2D에서 회전 금지
            agent.updateUpAxis   = false; // XY 평면 정의
            agent.speed          = speed;
        }

        SetAnimDirection(Vector2Int.zero);
    }

    public void CallDestroy()
    {
        // 좀비 사망 처리
        isLive = false;
        if (agent != null) agent.isStopped = true;
        SetAnimDirection(Vector2Int.zero);
    }

    void Update()
    {
        if (!isLive || agent == null || heroTr == null) return;

        float dt = Time.deltaTime;
        float dist = Vector2.Distance(transform.position, heroTr.position);

        // --------------------------------------------------
        // 1) 공격 범위 안이면 이동 중단 + 공격
        // --------------------------------------------------
        if (dist <= attackRange)
        {
            agent.isStopped = true; // 움직임 정지

            // 애니메이션 방향 계산
            Vector2 toHero = (Vector2)(heroTr.position - transform.position);
            Vector2Int dir = GetAnimDirFromVector(toHero);
            SetAnimDirection(dir);
            lastMoveDir = dir;

            // 처음 공격 진입 시 즉시 공격
            if (!inAttackRange)
            {
                DoAttack();
                attackTimer = attackDelay;
                inAttackRange = true;
                return;
            }

            // 공격 쿨타임 작동
            if (attackTimer > 0f) 
                attackTimer -= dt;
            else
            {
                DoAttack();
                attackTimer = attackDelay;
            }

            return;
        }
        else
        {
            // 공격 범위를 벗어남 → 다시 이동 가능
            inAttackRange = false;
            agent.isStopped = false;
        }

        // --------------------------------------------------
        // 2) 추적 범위 안이면 플레이어 추적
        // --------------------------------------------------
        if (dist <= chaseRange)
        {
            agent.SetDestination(heroTr.position);
        }
        else
        {
            // --------------------------------------------------
            // 3) 추적 범위 밖이면 배회 (랜덤 이동)
            // --------------------------------------------------
            wanderTimer -= dt;

            // 배회 타이밍이 되면 새로운 목적지 설정
            if (wanderTimer <= 0f || agent.remainingDistance <= 0.1f)
            {
                wanderTimer = wanderInterval;

                // 랜덤 위치 생성
                Vector2 rnd = Random.insideUnitCircle * wanderRadius;
                Vector3 guess = transform.position + new Vector3(rnd.x, rnd.y, 0f);

                // NavMesh 위에 존재하는지 체크 후 이동
                if (NavMesh.SamplePosition(guess, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }

        // --------------------------------------------------
        // 4) 애니메이션 방향 처리
        // --------------------------------------------------
        Vector3 v = agent.desiredVelocity;
        Vector2Int moveDir = GetAnimDirFromVector(v);

        if (moveDir == Vector2Int.zero)
            moveDir = lastMoveDir;

        SetAnimDirection(moveDir);
        lastMoveDir = moveDir;
    }

    // 실제 공격 처리
    void DoAttack()
    {
        if (heroStat != null && zombieStat != null)
            heroStat.decreaseHp(zombieStat.power);
    }

    // 4방향 애니메이션 설정
    void SetAnimDirection(Vector2Int dir)
    {
        bool left  = dir.x < 0;
        bool right = dir.x > 0;
        bool up    = dir.y > 0;
        bool down  = dir.y < 0;

        animator.SetBool("RIGHT", right);
        animator.SetBool("LEFT",  left);
        animator.SetBool("UP",    up);
        animator.SetBool("DOWN",  down);
    }

    // 4방향 결정(대각선 금지)
    Vector2Int GetAnimDirFromVector(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2Int.zero;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return new Vector2Int(v.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, v.y > 0 ? 1 : -1);
    }
}
