using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class HeroMoveControl : MonoBehaviour
{
    private Vector2 currentViewDirection = new Vector2(0f, -1f);
    public Vector2 CurrentViewDirection => currentViewDirection;

    [SerializeField] private float stepTime = 0.4f;
    public float moveSpeed = 2.5f;
    private const float minMoveSpeed = 0.5f;
    private const float maxMoveSpeed = 3f;

    private Rigidbody2D rb;
    private Vector2 targetPosition;

    public InputActionAsset inputActions;
    private InputAction moveAction;
    private Vector2 moveInput;

    private bool isMoving = false;

    [SerializeField] private Vector2 initHeroPosition = new Vector2(0.5f, 0.5f);
    [SerializeField] private Tilemap collisionTilemap;

    private Vector2Int currentReservedCell;
    private bool hasCurrentReservation = false;

    private Animator animator;
    private Vector2Int lastMoveDir = Vector2Int.down;

    // Animator 상태 이름 (Animator의 State 이름과 정확히 일치해야 함)
    private readonly int stIdleUp    = Animator.StringToHash("U");
    private readonly int stIdleDown  = Animator.StringToHash("D");
    private readonly int stIdleLeft  = Animator.StringToHash("L");
    private readonly int stIdleRight = Animator.StringToHash("R");

    private readonly int stWalkUp    = Animator.StringToHash("hero_Up");
    private readonly int stWalkDown  = Animator.StringToHash("hero_Down");
    private readonly int stWalkLeft  = Animator.StringToHash("hero_Left");
    private readonly int stWalkRight = Animator.StringToHash("hero_Right");

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (collisionTilemap == null)
        {
            var colGo = GameObject.Find("collision");
            if (colGo != null) collisionTilemap = colGo.GetComponent<Tilemap>();
        }

        moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");
        if (moveAction != null) moveAction.Enable();
        else Debug.LogError("Move 액션을 찾을 수 없습니다.");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.MovePosition(initHeroPosition);
        Vector2 snap = GetCellCenter(rb.position);
        rb.MovePosition(snap);
        targetPosition = snap;

        EnsureCurrentCellReserved();

        lastMoveDir = Vector2Int.down;
        UpdateAnimation(false);   // 아래 idle(D)로 시작
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        ReleaseReservation();
    }

    void OnDestroy()
    {
        ReleaseReservation();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput.x != 0f && moveInput.y != 0f)
            moveInput.x = 0f; // 대각선 방지
    }

    // ====== 유틸 ======

    Vector2 GetCellCenter(Vector2 worldPos)
    {
        if (collisionTilemap != null)
        {
            Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
            Vector3 c = collisionTilemap.GetCellCenterWorld(cell);
            return new Vector2(c.x, c.y);
        }
        return new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
    }

    Vector2Int WorldToCell(Vector2 worldPos)
    {
        if (collisionTilemap == null)
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

        Vector3Int c = collisionTilemap.WorldToCell(worldPos);
        return new Vector2Int(c.x, c.y);
    }

    Vector2 CellCenterWorld(Vector2Int cell)
    {
        if (collisionTilemap == null)
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        Vector3 c = collisionTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        return new Vector2(c.x, c.y);
    }

    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell);
    }

    bool CanStepTileOnly(Vector2 dirUnit)
    {
        Vector2 nextCenter = GetCellCenter(rb.position + dirUnit);
        return !IsBlockedCell(nextCenter);
    }

    void EnsureCurrentCellReserved()
    {
        if (collisionTilemap == null) return;

        Vector2Int cell = WorldToCell(rb.position);

        if (!hasCurrentReservation)
        {
            if (GridOccupancy.TryReserve(collisionTilemap, cell, this))
            {
                currentReservedCell = cell;
                hasCurrentReservation = true;
            }
            else
            {
                Debug.LogWarning($"[Hero] 현재 셀 예약 실패: {cell}");
            }
        }
        else if (cell != currentReservedCell)
        {
            GridOccupancy.Release(collisionTilemap, currentReservedCell, this);
            if (GridOccupancy.TryReserve(collisionTilemap, cell, this))
            {
                currentReservedCell = cell;
            }
            else
            {
                Debug.LogWarning($"[Hero] 현재 셀 재예약 실패: {cell}");
            }
        }
    }

    void ReleaseReservation()
    {
        if (!hasCurrentReservation || collisionTilemap == null) return;
        GridOccupancy.Release(collisionTilemap, currentReservedCell, this);
        hasCurrentReservation = false;
    }

    void changeViewDirection(Vector2 inputDir)
    {
        if (inputDir.sqrMagnitude < 0.0001f) return;
        Vector2 n = inputDir.normalized;
        if (currentViewDirection != n)
            currentViewDirection = n;
    }

    // === 애니메이션 직접 제어 ===
    void UpdateAnimation(bool moving)
    {
        int hashToPlay = stIdleDown; // 기본값

        if (lastMoveDir.y > 0)       hashToPlay = moving ? stWalkUp    : stIdleUp;
        else if (lastMoveDir.y < 0)  hashToPlay = moving ? stWalkDown  : stIdleDown;
        else if (lastMoveDir.x < 0)  hashToPlay = moving ? stWalkLeft  : stIdleLeft;
        else if (lastMoveDir.x > 0)  hashToPlay = moving ? stWalkRight : stIdleRight;

        animator.CrossFade(hashToPlay, 0.05f);
    }

    // ====== 메인 이동 루프 ======
    void FixedUpdate()
    {
        moveSpeed = 1f / stepTime;
        moveSpeed = Mathf.Clamp(moveSpeed, minMoveSpeed, maxMoveSpeed);

        // ---------- 이동 중 ----------
        if (isMoving)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            if (Vector2.Distance(rb.position, targetPosition) < 0.001f)
            {
                // 연속 입력
                if (moveInput.sqrMagnitude > 0.1f)
                {
                    Vector2 dir = moveInput.normalized;
                    dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

                    if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
                    {
                        Vector2 curCenter = CellCenterWorld(currentReservedCell);
                        Vector2Int nextCell = WorldToCell(curCenter + dir);

                        if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this))
                        {
                            if (hasCurrentReservation)
                                GridOccupancy.Release(collisionTilemap, currentReservedCell, this);

                            currentReservedCell = nextCell;
                            hasCurrentReservation = true;

                            targetPosition = CellCenterWorld(nextCell);
                            currentViewDirection = dir.normalized;

                            lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                            isMoving = true;
                            UpdateAnimation(true);  // 계속 걷기
                        }
                        else
                        {
                            // 다음 셀 점유 실패 → 방향만 바꾸고 멈춤
                            rb.MovePosition(targetPosition);
                            if (dir.sqrMagnitude > 0.1f)
                            {
                                lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                                changeViewDirection(dir);
                            }
                            isMoving = false;
                            UpdateAnimation(false); // 해당 방향 idle
                        }
                    }
                    else
                    {
                        // 벽 등으로 더 못감 → 방향만 돌고 멈춤
                        rb.MovePosition(targetPosition);
                        if (dir.sqrMagnitude > 0.1f)
                        {
                            lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                            changeViewDirection(dir);
                        }
                        isMoving = false;
                        UpdateAnimation(false);
                    }
                }
                else
                {
                    // 입력 없음 → 마지막 방향으로 서 있기
                    rb.MovePosition(targetPosition);
                    isMoving = false;
                    UpdateAnimation(false);
                }
            }

            return;
        }

        // ---------- 정지 상태 ----------
        if (moveInput.sqrMagnitude > 0.1f)
        {
            Vector2 dir = moveInput.normalized;
            dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

            if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
            {
                Vector2 curCenter = GetCellCenter(rb.position);
                Vector2Int nextCell = WorldToCell(curCenter + dir);

                if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this))
                {
                    if (hasCurrentReservation)
                        GridOccupancy.Release(collisionTilemap, currentReservedCell, this);

                    currentReservedCell = nextCell;
                    hasCurrentReservation = true;

                    rb.MovePosition(curCenter);
                    targetPosition = CellCenterWorld(nextCell);

                    lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                    changeViewDirection(dir);

                    isMoving = true;
                    UpdateAnimation(true); // 걷기 시작
                }
                else
                {
                    // 점유 때문에 못 움직임 → 방향만 돌리기
                    Vector2 center = GetCellCenter(rb.position);
                    rb.MovePosition(center);
                    targetPosition = center;

                    if (dir.sqrMagnitude > 0.1f)
                    {
                        lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                        changeViewDirection(dir);
                    }

                    isMoving = false;
                    UpdateAnimation(false);
                }
            }
            else
            {
                // 벽 때문에 못 움직임 → 방향만 돌리기
                Vector2 curCenter = GetCellCenter(rb.position);
                rb.MovePosition(curCenter);
                targetPosition = curCenter;

                if (dir.sqrMagnitude > 0.1f)
                {
                    lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                    changeViewDirection(dir);
                }

                isMoving = false;
                UpdateAnimation(false);
            }
        }
        else
        {
            // 입력 없음 → 현재 셀 점유만 유지, idle 방향 유지
            EnsureCurrentCellReserved();
        }
    }
}