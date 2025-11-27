using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HeroMoveControl : MonoBehaviour
{
    public static HeroMoveControl Instance { get; private set; }

    // 현재 바라보는 방향 (기본값: 아래)
    private Vector2 currentViewDirection = new Vector2(0f, -1f);
    public Vector2 CurrentViewDirection => currentViewDirection;

    [SerializeField] private float stepTime = 0.4f; // 속도 계산용 파라미터

    private float moveSpeed;
    private const float minMoveSpeed = 0.5f;
    private const float maxMoveSpeed = 6f;

    private Rigidbody2D rb;

    // (필요하면 다른 액션에 쓰라고 남겨둠 – 이동은 Keyboard로 처리)
    public InputActionAsset inputActions;

    private Vector2 moveInput; // 최종 입력 방향

    [SerializeField] private Vector2 initHeroPosition = new Vector2(0.5f, 0.5f);

    private Animator animator;
    private Vector2Int lastMoveDir = Vector2Int.down;

    // Idle
    private readonly int stIdleUp    = Animator.StringToHash("U");
    private readonly int stIdleDown  = Animator.StringToHash("D");
    private readonly int stIdleLeft  = Animator.StringToHash("L");
    private readonly int stIdleRight = Animator.StringToHash("R");

    // Walk
    private readonly int stWalkUp    = Animator.StringToHash("hero_Up");
    private readonly int stWalkDown  = Animator.StringToHash("hero_Down");
    private readonly int stWalkLeft  = Animator.StringToHash("hero_Left");
    private readonly int stWalkRight = Animator.StringToHash("hero_Right");

    // 방향 전환 빠르게
    private const float animCrossFadeTime = 0.02f;

    // ====== 구르기 관련 ======
    [Header("Roll (Dodge) Settings")]
    [SerializeField] private float rollSpeed = 8f;       // 구르기 속도
    [SerializeField] private float rollDuration = 0.25f; // 구르기 유지 시간
    [SerializeField] private float rollCooldown = 0.5f;  // 구르기 쿨타임

    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private Vector2 rollDirection = Vector2.zero;
    // =========================

    void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            Debug.LogError("[HeroMoveControl] Rigidbody2D가 없습니다.");
    }

    void Start()
    {
        if (rb != null)
        {
            rb.MovePosition(initHeroPosition);
            lastMoveDir = Vector2Int.down;
            UpdateAnimation(false);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Hero] 씬 로드 완료: {scene.name}");

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        UpdateAnimation(false);
    }

    // === 입력 처리: Keyboard에서 직접 읽기 ===
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

        moveInput = dir;
    }

    // 구르기 시작 시도 (F키)
    void TryStartRoll()
    {
        if (isRolling) return;
        if (rollCooldownTimer > 0f) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.fKey.wasPressedThisFrame)
        {
            // 방향: 입력이 있으면 그 방향, 아니면 바라보는 방향
            Vector2 dir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : currentViewDirection;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.down; // 완전 제로면 아래로 기본

            rollDirection = dir;
            isRolling = true;
            rollTimer = rollDuration;
            rollCooldownTimer = rollCooldown;

            // 방향 갱신
            lastMoveDir = new Vector2Int(
                Mathf.RoundToInt(rollDirection.x),
                Mathf.RoundToInt(rollDirection.y)
            );
            changeViewDirection(rollDirection);
        }
    }

    void Update()
    {
        // 키 입력 읽기 (매 프레임)
        ReadKeyboardInput();

        // 구르기 쿨타임 감소
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        // F키 입력 체크해서 구르기 시작
        TryStartRoll();
    }

    // 바라보는 방향 갱신
    void changeViewDirection(Vector2 inputDir)
    {
        if (inputDir.sqrMagnitude < 0.0001f) return;
        Vector2 n = inputDir.normalized;
        if (currentViewDirection != n)
            currentViewDirection = n;
    }

    // 애니메이션 처리
    void UpdateAnimation(bool moving)
    {
        int hashToPlay = stIdleDown;

        if (lastMoveDir.y > 0)       hashToPlay = moving ? stWalkUp    : stIdleUp;
        else if (lastMoveDir.y < 0)  hashToPlay = moving ? stWalkDown  : stIdleDown;
        else if (lastMoveDir.x < 0)  hashToPlay = moving ? stWalkLeft  : stIdleLeft;
        else if (lastMoveDir.x > 0)  hashToPlay = moving ? stWalkRight : stIdleRight;

        animator.CrossFade(hashToPlay, animCrossFadeTime);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // HeroStat 기반 속도 계산
        var stat = GetComponent<HeroStat>();
        if (stat != null)
        {
            moveSpeed = 1f / stepTime * stat.speed / 1000f;
        }
        else
        {
            moveSpeed = 2.5f; // HeroStat 없으면 기본값
        }

        moveSpeed = Mathf.Clamp(moveSpeed, minMoveSpeed, maxMoveSpeed);

        // ====== 구르기 중인 경우 먼저 처리 ======
        if (isRolling)
        {
            // 구르기 이동
            rb.linearVelocity = rollDirection * rollSpeed;

            rollTimer -= Time.fixedDeltaTime;
            if (rollTimer <= 0f)
            {
                isRolling = false;

                // 구르기 끝나고 입력이 없으면 멈춤
                if (moveInput.sqrMagnitude <= 0.01f)
                {
                    rb.linearVelocity = Vector2.zero;
                    UpdateAnimation(false);
                    return;
                }
            }

            // 구르는 동안에도 걷기 애니처럼 재생 (원하면 나중에 롤 애니 따로 빼도 됨)
            UpdateAnimation(true);
            return;
        }
        // ========================================

        // 입력이 없을 때: 무조건 정지
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        // 입력이 있을 때 (원래 이동 로직)
        Vector2 dir = moveInput.normalized;
        dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y)); // -1, 0, 1

        if (dir.sqrMagnitude <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        // 실제 이동
        rb.linearVelocity = dir * moveSpeed;

        // 방향/시선 갱신
        lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
        changeViewDirection(dir);

        // 걷기 애니메이션
        UpdateAnimation(true);
    }

    // 외부에서 타겟 위치를 강제로 설정할 때 사용 (워프 등)
    public void SetTargetPosition(Vector2 pos)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.position = pos;
        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(false);
    }

    // 즉시 텔레포트
    public void ForceMove(Vector2 newPos)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.position = newPos;
        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(false);
    }

    // 쉘터에서 나갈 때 외부 위치로 강제 이동
    public void ExitShelter(Vector3 outsidePos)
    {
        ForceMove(outsidePos);
    }
}
