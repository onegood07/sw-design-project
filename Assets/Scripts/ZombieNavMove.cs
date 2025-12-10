using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMove : MonoBehaviour
{
    [Header("이동 / 탐지")]
    [SerializeField] float speed = 3f;            // NavMeshAgent 기본 이동 속도
    [SerializeField] float chaseRange = 5f;       // 플레이어 추적을 시작하는 거리
    [SerializeField] float attackRange = 1.0f;    // 공격이 가능해지는 거리
    [SerializeField] float attackDelay = 1.0f;    // 공격 쿨타임 (1.0초)

    [Header("배회 설정")]
    [SerializeField] float wanderRadius = 2f;     // 배회 목적지의 랜덤 반경
    [SerializeField] float wanderInterval = 2f;   // 배회 목적지를 다시 설정하는 주기

    [Header("애니메이션 / 피격 연출")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Color hitColor = Color.red;
    [SerializeField] float hitStopDuration = 0.3f;

    [Header("공격 판정 오프셋")]
    [SerializeField] Vector2 zombieAttackOffset = new Vector2(0f, 0.5f);
    [SerializeField] Vector2 heroAttackOffset = new Vector2(0f, 0.5f);
    [Header("공격할 때 사운드")]
    [SerializeField]private AudioClip attackClip;
    Animator animator;
    int paramAttack = Animator.StringToHash("Attack");
    [Header("애니메이션 원본 길이")]
    [SerializeField] float attackClipDuration = 0.417f; // 애니메이션 원본 길이
    Coroutine attackRoutine;
    bool isAttacking = false;
    float originalAnimatorSpeed = 1f;
    Color originalColor;
    Coroutine hitStopRoutine;
    bool isHitStopped = false;
    NavMeshAgent agent;
    Transform heroTr;
    HeroStat heroStat;
    ZombieStat zombieStat;

    bool isLive = true;           // 좀비 생존 여부
    bool inAttackRange = false;   // 공격 범위에 들어온 첫 프레임 체크용
    float attackTimer = 0f;       // 공격 딜레이 타이머
    float wanderTimer = 0f;       // 배회 목적지 갱신 타이머
    Vector2Int lastMoveDir = Vector2Int.down; // 애니메이션에 사용할 마지막 이동 방향
    // public bool isInDialogue = false; // 플레이어가 NPC와 대화 중인지 확인

    void Awake()
    {
        animator   = GetComponent<Animator>();
        agent      = GetComponent<NavMeshAgent>();
        zombieStat = GetComponent<ZombieStat>();

        // 플레이어 탐색
        var heroGo = GameObject.Find("Hero");
        if (heroGo != null)
        {
            heroTr   = heroGo.transform;
            heroStat = heroGo.GetComponent<HeroStat>();
        }

        // NavMeshAgent를 2D 전용으로 설정
        if (agent != null)
        {
            agent.updateRotation = false;   // 회전 비활성화
            agent.updateUpAxis   = false;   // Y축을 위쪽으로 사용
            agent.speed          = speed;   // 이동 속도 적용
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // 처음엔 Idle 방향으로 설정
        SetAnimDirection(Vector2Int.zero);

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            hitStopRoutine = null;
        }
    }

    // ZombieStat에서 사망 호출 시 실행
    public void CallDestroy()
    {
        isLive = false;
        if (agent != null)
            agent.isStopped = true;   // 이동 정지

        SetAnimDirection(Vector2Int.zero);

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            hitStopRoutine = null;
        }

        isHitStopped = false;
        ResetSpriteColor();
    }

   void Update()
{
    bool dialogueActive = GameManager.Instance != null && GameManager.Instance.IsDialogueActive;

   if (dialogueActive)
    {
        // 이미 멈춰있을 수 있지만, 매 프레임 확실히 확인
        if (agent != null && !agent.isStopped) 
            agent.isStopped = true; 
        
        // 정지 상태 애니메이션을 위해 현재 방향으로 설정
        SetAnimDirection(lastMoveDir); 
        
        return;
    }

    if (!isLive || agent == null || heroTr == null)
        return;

    if (isHitStopped)
    {
        if (agent != null && !agent.isStopped)
            agent.isStopped = true;

        SetAnimDirection(lastMoveDir);
        return;
    }

    float dt = Time.deltaTime;
    // 핏값 기준점(발 위치)을 보정하기 위해 오프셋을 적용한 위치에서 거리 계산
    Vector2 zombiePos = new Vector2(transform.position.x, transform.position.y) + zombieAttackOffset;
    Vector2 heroPos = new Vector2(heroTr.position.x, heroTr.position.y) + heroAttackOffset;
    float dist = Vector2.Distance(zombiePos, heroPos);

    // 1) 플레이어가 공격 사거리 안에 있으면 → 이동 중단 + 공격 처리
    if (dist <= attackRange)
    {
        if (!agent.isStopped)
            agent.isStopped = true;

        if (agent.hasPath)
            agent.ResetPath();

        // 공격을 위해 플레이어 방향으로 애니메이션 방향만 돌리기
        Vector2 toHero = (Vector2)(heroTr.position - transform.position);
        Vector2Int dir = GetAnimDirFromVector(toHero);
        SetAnimDirection(dir);
        lastMoveDir = dir;

        // 첫 진입이면 즉시 공격
        if (!inAttackRange)
        {
            DoAttack();
            PlayAttackAnimation();
            attackTimer = attackDelay; // 쿨타임 시작
            inAttackRange = true;
            return;
        }

        // 쿨타임 감소
        if (attackTimer > 0f)
            attackTimer -= dt;
        else
        {
            // 공격 실행 후 쿨타임 리셋
            DoAttack();
            PlayAttackAnimation();
            attackTimer = attackDelay;
        }

        return;
    }
    else
    {
        // 공격 범위를 벗어나면 다시 이동 가능
        inAttackRange = false;
        if (!isAttacking)
            agent.isStopped = false;
    }

    // 2) 추적 범위 안 → 플레이어를 계속 추적
    if (dist <= chaseRange)
    {
        agent.SetDestination(heroTr.position);
    }
    else
    {
        // 3) 추적 범위 밖 → 랜덤 배회 AI
        wanderTimer -= dt;

        // 일정 시간마다 새 목적지 생성
        if (wanderTimer <= 0f || agent.remainingDistance <= 0.1f)
        {
            wanderTimer = wanderInterval;

            // 랜덤 방향/위치 생성
            Vector2 rnd = Random.insideUnitCircle * wanderRadius;
            Vector3 guess = transform.position + new Vector3(rnd.x, rnd.y, 0f);

            // NavMesh 위에 위치할 경우만 이동
            if (NavMesh.SamplePosition(guess, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    // 4) 실제 이동 벡터(v)를 이용해서 4방향 애니메이션 갱신
    Vector3 v = agent.desiredVelocity;            // NavMeshAgent가 향하고 있는 실제 이동 벡터
    Vector2Int moveDir = GetAnimDirFromVector(v); // 4방향으로 변환

    if (moveDir == Vector2Int.zero)               // 거의 멈춘 경우, 이전 방향 유지
        moveDir = lastMoveDir;

    SetAnimDirection(moveDir);
    lastMoveDir = moveDir;
}

    // 실제 데미지 처리(좀비 공격)
    void DoAttack()
    {
        if (heroStat != null && zombieStat != null)
        {
            heroStat.decreaseHp(zombieStat.power);

            // 사운드 매니저와 클립이 유효할 때만 공격 사운드를 재생
            if (SoundManager.Instance != null && attackClip != null)
            {
                SoundManager.Instance.PlaySFX(attackClip, 1.0f);
            }
        }
    }

    void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        if (!isAttacking)
            originalAnimatorSpeed = animator.speed;

        animator.speed = 1f;
        animator.SetBool(paramAttack, true);
        isAttacking = true;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(ResetAttackFlag());
    }

    IEnumerator ResetAttackFlag()
    {
        yield return new WaitForSeconds(Mathf.Max(attackClipDuration, 0.01f));

        if (animator != null)
            animator.SetBool(paramAttack, false);

        isAttacking = false;
        animator.speed = originalAnimatorSpeed;

        attackRoutine = null;
    }

    public void OnDamageTaken()
    {
        if (!isLive)
            return;

        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);

        hitStopRoutine = StartCoroutine(HitStopRoutine());
    }

    // Animator에 오른쪽/왼쪽/위/아래 방향을 전달
    void SetAnimDirection(Vector2Int dir)
    {
        animator.SetBool("RIGHT", dir.x > 0);
        animator.SetBool("LEFT",  dir.x < 0);
        animator.SetBool("UP",    dir.y > 0);
        animator.SetBool("DOWN",  dir.y < 0);
    }

    // 벡터를 4방향 중 하나로 변환
    Vector2Int GetAnimDirFromVector(Vector2 v)
    {
        if (v == Vector2.zero)
            return Vector2Int.zero;

        // x가 크면 좌우, y가 크면 상하 방향
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return new Vector2Int(v.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, v.y > 0 ? 1 : -1);
    }

    IEnumerator HitStopRoutine()
    {
        isHitStopped = true;

        if (agent != null && !agent.isStopped)
            agent.isStopped = true;

        if (spriteRenderer != null)
            spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(hitStopDuration);

        ResetSpriteColor();

        isHitStopped = false;
        hitStopRoutine = null;
    }

    void ResetSpriteColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}
