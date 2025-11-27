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
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private Vector2 moveInput;

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

    // 방향 전환이 느려 보였던 원인: CrossFade 시간 + 상태 전환 시간
    // → CrossFade 시간을 줄여서 더 "즉각" 바뀌게 한다.
    private const float animCrossFadeTime = 0.02f; // ★ 방향전환 더 빠르게 (기존 0.05f)

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

        // InputAction 세팅
        if (inputActions != null)
        {
            moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");
            if (moveAction != null)
                moveAction.Enable();
            else
                Debug.LogError("Move 액션을 찾을 수 없습니다.");
        }
        else
        {
            Debug.LogError("InputActionAsset이 설정되지 않았습니다.");
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.MovePosition(initHeroPosition);
        lastMoveDir = Vector2Int.down;
        UpdateAnimation(false);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (moveAction != null) moveAction.Enable();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (moveAction != null) moveAction.Disable();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Hero] 씬 로드 완료: {scene.name}");

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // 씬 로드 후 기본 상태 정리
        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(false);
    }

    // New Input System 콜백
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();


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

        // ★ CrossFade 시간을 줄여서 방향 전환을 더 빠르게
        animator.CrossFade(hashToPlay, animCrossFadeTime);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // HeroStat 기반 속도 계산 (기존 공식 유지)
        var stat = GetComponent<HeroStat>();
        if (stat != null)
        {
            moveSpeed = 1f / stepTime * stat.speed / 1000f;
        }
        else
        {
            moveSpeed = 2.5f; // 혹시 HeroStat 없으면 기본값
        }

        moveSpeed = Mathf.Clamp(moveSpeed, minMoveSpeed, maxMoveSpeed);

        // 현재 실제 속도 (좀비한테 밀리는 것까지 포함)
        Vector2 currentVel = rb.linearVelocity;

        // ===========================================
        // 입력이 없을 때 처리 (좀비에게 밀릴 때 애니 안 끊기게)
        // ===========================================
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            // 1) 실제로 밀려서 움직이고 있는 경우 → 걷는 애니 유지
            if (currentVel.sqrMagnitude > 0.001f)
            {
                Vector2 pushDir = currentVel.normalized;

                // 밀리는 방향 기준으로 lastMoveDir 업데이트
                int lx = lastMoveDir.x;
                int ly = lastMoveDir.y;

                if (Mathf.Abs(pushDir.x) > Mathf.Abs(pushDir.y))
                {
                    // 좌우로 더 많이 움직이면 X 기준
                    lx = pushDir.x > 0 ? 1 : -1;
                    ly = 0;
                }
                else
                {
                    // 위아래로 더 많이 움직이면 Y 기준
                    ly = pushDir.y > 0 ? 1 : -1;
                    lx = 0;
                }

                lastMoveDir = new Vector2Int(lx, ly);

                UpdateAnimation(true); // 걷기 애니메이션 계속
            }
            else
            {
                // 2) 정말로 멈춰 있을 때만 Idle 애니메이션
                rb.linearVelocity = Vector2.zero;
                UpdateAnimation(false);
            }
            return;
        }

        // ===========================================
        // 입력이 있을 때 (원래 이동 로직)
        // ===========================================
        // 방향 처리 (4방향)
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
