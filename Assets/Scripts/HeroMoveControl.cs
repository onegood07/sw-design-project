using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HeroMoveControl : MonoBehaviour
{
    public static HeroMoveControl Instance { get; private set; }

    // 현재 바라보는 방향 (기본값: 아래 방향)
    private Vector2 currentViewDirection = new Vector2(0f, -1f);
    public Vector2 CurrentViewDirection => currentViewDirection;

    // private float stepTime = 0.4f; // HeroStat.speed를 이용해 실제 속도를 계산할 때 쓰는 기준 시간 (현재 사용하지 않음)

    private float moveSpeed;                         // 최종 이동 속도
    // 허기 패널티(예: speed 100 → 0.25f, 신발로 200 → 0.5f)에서도
    // 신발 효과(배수 차이)가 느껴지도록 최소값을 너무 높지 않게 설정한다.
    private const float minMoveSpeed = 0.25f;        // 이동 속도의 최소값
    private const float maxMoveSpeed = 6f;           // 이동 속도의 최대값

    private Rigidbody2D rb;                          // 물리 이동을 위한 Rigidbody2D

    // 현재는 이동에는 사용하지 않지만, 다른 입력 액션용으로 남겨둔 InputActionAsset
    public InputActionAsset inputActions;

    private Vector2 moveInput;                       // 키보드 입력으로부터 얻은 이동 방향 (WASD)

    [SerializeField] private Vector2 initHeroPosition = new Vector2(0.5f, 0.5f); // 시작 시 히어로의 초기 위치

    private Animator animator;                       // 애니메이션 제어용 Animator
    private Vector2Int lastMoveDir = Vector2Int.down; // 마지막 이동 방향(정수화된 방향, 애니메이션 선택용)

    // Hero 하위 총 스프라이트(Pistol_S/N/W/E)를 관리하는 컴포넌트
    private HeroWeaponVisual weaponVisual; // HeroWeaponVisual 클래스가 외부에서 정의되어 있다고 가정합니다.

    // Idle 상태 애니메이션 해시값
    private readonly int stIdleUp    = Animator.StringToHash("U");
    private readonly int stIdleDown  = Animator.StringToHash("D");
    private readonly int stIdleLeft  = Animator.StringToHash("L");
    private readonly int stIdleRight = Animator.StringToHash("R");

    // Walk 상태 애니메이션 해시값
    private readonly int stWalkUp    = Animator.StringToHash("hero_Up");
    private readonly int stWalkDown  = Animator.StringToHash("hero_Down");
    private readonly int stWalkLeft  = Animator.StringToHash("hero_Left");
    private readonly int stWalkRight = Animator.StringToHash("hero_Right");

    private readonly int paramUp    = Animator.StringToHash("UP");
    private readonly int paramDown  = Animator.StringToHash("DOWN");
    private readonly int paramLeft  = Animator.StringToHash("LEFT");
    private readonly int paramRight = Animator.StringToHash("RIGHT");

    // Gun Attack Walk
    private readonly int stAttackUp    = Animator.StringToHash("Up_attack");
    private readonly int stAttackDown  = Animator.StringToHash("Down_attack");
    private readonly int stAttackLeft  = Animator.StringToHash("Left_attack");
    private readonly int stAttackRight = Animator.StringToHash("Right_attack");

    // Gun Attack Idle
    private readonly int stAttackIdleUp    = Animator.StringToHash("U_attack");
    private readonly int stAttackIdleDown  = Animator.StringToHash("D_attack");
    private readonly int stAttackIdleLeft  = Animator.StringToHash("L_attack");
    private readonly int stAttackIdleRight = Animator.StringToHash("R_attack");
    
    // Roll 애니메이션 해시값
    private readonly int stRollUp    = Animator.StringToHash("hero_Up_Roll");
    private readonly int stRollDown  = Animator.StringToHash("hero_Down_Roll");
    private readonly int stRollLeft  = Animator.StringToHash("hero_Left_Roll");
    private readonly int stRollRight = Animator.StringToHash("hero_Right_Roll");

    private const float animCrossFadeTime = 0.02f;

    [Header("Attack Animation")]
    [SerializeField] private float attackAnimDuration = 0.5f;

    private const string paramAttack = "Attack";
    private Coroutine attackResetRoutine;
    private bool isAttackAnimating;
    private float attackLockTimer = 0f;

    // ---- 구르기(회피) 관련 설정 ----
    [Header("Roll (Dodge) Settings")]
    [SerializeField] private float rollSpeed = 8f;       // 구르기 속도
    [SerializeField] private float rollDuration = 0.25f; // 구르기가 유지되는 시간(초)
    [SerializeField] private float rollCooldown = 2f;    // 구르기 후 다시 사용할 수 있을 때까지의 쿨타임(초)

    private bool isRolling = false;          // 현재 구르기 중인지 여부
    private float rollTimer = 0f;            // 남은 구르기 시간
    private float rollCooldownTimer = 0f;    // 남은 쿨타임 시간
    private Vector2 rollDirection = Vector2.zero; // 구르기 진행 방향

    void Awake()
    {
        // 싱글톤 패턴 적용: 이미 인스턴스가 있으면 자기 자신 파괴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);   // 씬 전환 시에도 파괴되지 않도록 유지
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        weaponVisual = GetComponent<HeroWeaponVisual>();

        if (rb == null)
            Debug.LogError("[HeroMoveControl] Rigidbody2D가 없습니다.");
    }

    void Start()
    {
        // 시작 시 초기 위치로 이동시키고, 아래 방향 idle 상태로 맞춤
        if (rb != null)
        {
            rb.MovePosition(initHeroPosition);
            lastMoveDir = Vector2Int.down;
            UpdateAnimation(false); // Idle 애니메이션 재생
        }
    }

    void OnEnable()
    {
        // 씬이 새로 로드될 때 호출될 콜백 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 씬 로드 콜백 해제 (중복 등록 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // 씬 로드 시 위치 조정은 FadeManager에서 처리한다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Hero] 씬 로드 완료: {scene.name}");

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // 씬 로드 시 이동 속도를 0으로 초기화
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 위치 조정은 FadeManager에서 처리하며, 여기서는 Idle 상태로만 전환
        UpdateAnimation(false);
    }

    // 키보드 입력(WASD)을 직접 읽어서 moveInput에 반영
    void ReadKeyboardInput()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 dir = Vector2.zero;

        if (kb.wKey.isPressed) dir.y += 1;
        if (kb.sKey.isPressed) dir.y -= 1;
        if (kb.aKey.isPressed) dir.x -= 1;
        if (kb.dKey.isPressed) dir.x += 1;

        // WASD 조합 방향(대각 포함)
        moveInput = dir;
    }
    void PlayRollAnimation()
    {
        int rollHash = stRollDown;

        // rollDirection 기준으로 방향 선택
        if (rollDirection.y > 0)       rollHash = stRollUp;
        else if (rollDirection.y < 0)  rollHash = stRollDown;
        else if (rollDirection.x < 0)  rollHash = stRollLeft;
        else if (rollDirection.x > 0)  rollHash = stRollRight;

        animator.CrossFade(rollHash, animCrossFadeTime);
    }

    // 스페이스바로 구르기를 시작할 수 있는지 검사하고, 가능하면 구르기 상태로 전환
    void TryStartRoll()
    {
        // 이미 구르는 중이거나 쿨타임이 남아있으면 시작 불가
        if (isRolling || rollCooldownTimer > 0f) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // 이번 프레임에 스페이스바가 "눌린 순간"인지 확인
        if (kb.spaceKey.wasPressedThisFrame)
        {
            // 구르기 방향: 이동 입력이 없으면 현재 바라보는 방향 사용
            Vector2 dir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : currentViewDirection;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.down; // 완전 제로인 경우에는 기본값으로 아래 방향 사용

            rollDirection = dir;
            isRolling = true;
            rollTimer = rollDuration;                    // 구르기 지속 시간
            rollCooldownTimer = Mathf.Max(rollCooldown, 2f);  // 최소 2초 쿨타임

            // 구르기 시작하는 동안에는 총을 숨긴다.
            if (weaponVisual != null)
            {
                weaponVisual.SetRolling(true);
            }

            // 구르기 방향 기준으로 마지막 방향/시선 갱신
            lastMoveDir = new Vector2Int(
                Mathf.RoundToInt(rollDirection.x),
                Mathf.RoundToInt(rollDirection.y)
            );
            changeViewDirection(rollDirection);
        }
    }

    void Update()
    {
        attackLockTimer -= Time.deltaTime;
        
        // 매 프레임 키보드 입력을 읽어 moveInput 갱신
        ReadKeyboardInput();

        // 남은 구르기 쿨타임 감소
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        // 스페이스 입력 확인 후, 구르기 시작 시도
        TryStartRoll();
    }

    // 현재 바라보는 방향 벡터를 갱신
    void changeViewDirection(Vector2 inputDir)
    {
        if (inputDir.sqrMagnitude < 0.0001f) return; // 거의 0인 벡터는 무시
        Vector2 n = inputDir.normalized;
        if (currentViewDirection != n)
        {
            currentViewDirection = n;

            // 무기 방향 스프라이트도 함께 갱신
            if (weaponVisual != null)
            {
                weaponVisual.UpdateDirection(currentViewDirection);
            }
        }
    }

    // 이동/정지 상태와 마지막 방향에 따라 적절한 애니메이션 상태로 전환
    void UpdateAnimation(bool moving)
    {
        if (isAttackAnimating)
            return;

        animator.SetBool("IsMoving", moving);

        bool hasGun = animator.GetInteger(paramAttack) == 1;

        int hashToPlay = stIdleDown;

        if (hasGun)
        {
            // 총 든 상태의 걷기/Idle 애니메이션
            if (lastMoveDir.y > 0)       hashToPlay = moving ? stAttackUp    : stAttackIdleUp;
            else if (lastMoveDir.y < 0)  hashToPlay = moving ? stAttackDown  : stAttackIdleDown;
            else if (lastMoveDir.x < 0)  hashToPlay = moving ? stAttackLeft  : stAttackIdleLeft;
            else if (lastMoveDir.x > 0)  hashToPlay = moving ? stAttackRight : stAttackIdleRight;

            animator.CrossFade(hashToPlay, animCrossFadeTime);
            return;
        }

        // 기본 애니메이션 (총을 들지 않은 상태)
        if (lastMoveDir.y > 0)       hashToPlay = moving ? stWalkUp    : stIdleUp;
        else if (lastMoveDir.y < 0)  hashToPlay = moving ? stWalkDown  : stIdleDown;
        else if (lastMoveDir.x < 0)  hashToPlay = moving ? stWalkLeft  : stIdleLeft;
        else if (lastMoveDir.x > 0)  hashToPlay = moving ? stWalkRight : stIdleRight;

        animator.CrossFade(hashToPlay, animCrossFadeTime);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // HeroStat를 가져와서 스탯 기반 이동 속도 계산
        // baseSpeed(기본 1000)일 때 2.5f가 되도록 비율로 환산해서 사용
        var stat = HeroStat.Instance;
        if (stat != null)
        {
            // 1) 장비/물약 등으로 인해 변경된 현재 speed 값을 가져온다.
            float effectiveSpeed = stat.speed;

            // 2) 허기가 100 이하일 때는, "현재 속도"에 0.1배 패널티를 먼저 적용한다.
            //    예) 기본 1000 → 100, 신발로 2배(2000) → 200 (허기 상태에서도 x2 유지)
            if (stat.hunger <= 100f)
            {
                effectiveSpeed *= 0.1f;
            }

            // 3) 최종 이동속도는 baseSpeed 대비 비율로 환산해서 사용
            float ratio = stat.baseSpeed > 0f ? effectiveSpeed / stat.baseSpeed : 1f;
            moveSpeed = 2.5f * Mathf.Max(ratio, 0.1f);
        }
        else
        {
            // HeroStat을 찾지 못한 경우에는 기본값 사용
            moveSpeed = 2.5f;
        }

        // 이동 속도를 최소/최대 범위 안으로 제한
        moveSpeed = Mathf.Clamp(moveSpeed, minMoveSpeed, maxMoveSpeed);

        // 1) 구르기 중인 경우
        if (isRolling)
        {
            rb.linearVelocity = rollDirection * rollSpeed;

            rollTimer -= Time.fixedDeltaTime;
            if (rollTimer <= 0f)
            {
                isRolling = false;
                animator.SetBool("ROLL", false);

                // 구르기가 끝나면, 무기 장착 상태/방향에 맞게 다시 총을 보이게 할 수 있도록 롤링 해제
                if (weaponVisual != null)
                {
                    weaponVisual.SetRolling(false);
                }

                // 구르기 종료 후 입력이 없으면 즉시 멈추고 Idle 애니메이션
                if (moveInput.sqrMagnitude <= 0.01f)
                {
                    rb.linearVelocity = Vector2.zero;
                    UpdateAnimation(false);
                    return;
                }
            }

            PlayRollAnimation();
            return;
        }

        // 2) 구르기 중이 아니고, 입력도 없는 경우 → 정지
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        // 3) 일반 이동 처리(WASD)
        Vector2 dir = moveInput.normalized;
        dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y)); // -1, 0, 1

        if (dir.sqrMagnitude <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        rb.linearVelocity = dir * moveSpeed;

        lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
        changeViewDirection(dir);

        UpdateAnimation(true);
    }

    // (공격 애니메이션 로직 생략)
    public void TriggerAttackAnimation()
    {
        if (animator == null || attackLockTimer > 0f)
            return;

        attackLockTimer = attackAnimDuration;
        if (attackResetRoutine != null)
            StopCoroutine(attackResetRoutine);

        animator.SetInteger(paramAttack, 1);
        isAttackAnimating = true;
        attackResetRoutine = StartCoroutine(ResetAttackFlagAfterDelay());
    }

    public void OnAttackAnimationFinished()
    {
        if (attackResetRoutine != null)
        {
            StopCoroutine(attackResetRoutine);
            attackResetRoutine = null;
        }

        ResetAttackState();
    }

    IEnumerator ResetAttackFlagAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(attackAnimDuration, 0.01f));
        attackResetRoutine = null;
        ResetAttackState();
    }

    void ResetAttackState()
    {
        if (animator != null)
            animator.SetInteger(paramAttack, 0);

        isAttackAnimating = false;

        bool isCurrentlyMoving = moveInput.sqrMagnitude > 0.01f;
        UpdateAnimation(isCurrentlyMoving);
    }

    // 총 장착/해제 
    public void SetAttackEquipState(bool value)
    {
        if (animator == null)
            return;

        Debug.Log($"[HeroMoveControl] SetAttackEquipState -> {value}");
        animator.SetInteger(paramAttack, value ? 1 : 0);

        // 무기 장착 여부에 따라 Hero 하위 Pistol_* 오브젝트의 표시 상태 갱신
        if (weaponVisual != null)
        {
            weaponVisual.SetWeaponEquipped(value);
        }

        if (!value && attackResetRoutine != null)
        {
            StopCoroutine(attackResetRoutine);
            attackResetRoutine = null;
        }

        if (!value)
            isAttackAnimating = false;
    }

    // 외부에서 특정 위치로 워프할 때 사용 (타겟 위치로 순간 이동)
    public void SetTargetPosition(Vector2 pos)
    {
        ForceMove(pos);
    }

    // 강제 텔레포트 함수 (FadeManager에서 호출)
    public void ForceMove(Vector2 newPos)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.position = newPos;
        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(false);
    }

    // 쉘터에서 나갈 때 외부 위치로 강제 이동시킬 때 사용 (ForceMove와 동일)
    public void ExitShelter(Vector3 outsidePos)
    {
        ForceMove(outsidePos);
    }
}