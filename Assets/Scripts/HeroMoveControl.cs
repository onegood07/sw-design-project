using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class HeroMoveControl : MonoBehaviour
{
    private Vector2 currentViewDirection = new Vector2(0f, -1f); // 시야 방향. 외부 참조 변수
    public Vector2 CurrentViewDirection => currentViewDirection;

    public float moveSpeed = 5f; // 초기 이동 속도
    private Rigidbody2D rb;       // 2D 물리 엔진 컴포넌트 참조
    private Vector2 targetPosition; // 이동 목표(그리드 센터)

    // Input Actions Asset을 public으로 참조
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private Vector2 moveInput;

    private bool isMoving = false; // 그리드 단위 이동 중 여부

    [SerializeField] private Vector2 initHeroPosition = new Vector2(0.5f, 0.5f); // Hero 초기화 좌표
    [SerializeField] private Tilemap collisionTilemap;

    // ===== [ADD] 점유(Reservation) 관리용 필드 =====
    private Vector2Int currentReservedCell;     // 내가 현재 점유 중인 셀
    private bool hasCurrentReservation = false; // 현재 셀을 점유했는가
    private Vector2Int? nextReservedCell = null; // 다음 셀을 선점했으면 저장

    void Awake()
    {
        // 충돌맵 찾아서 넣어주기
        collisionTilemap = GameObject.Find("collision")?.GetComponent<Tilemap>();

        // Action Map에서 Move 액션 가져오기
        moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");
        if (moveAction != null) moveAction.Enable();
        else Debug.LogError("Move 액션을 찾을 수 없습니다");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.MovePosition(initHeroPosition); // 그리드 내부로 Hero 위치 초기화

        // >>> [ADD] 시작 위치 스냅 + 현재 셀 예약 보장
        Vector2 snap = GetCellCenter(rb.position);
        rb.MovePosition(snap);
        EnsureCurrentCellReserved(); // 현재 서 있는 칸을 점유
        targetPosition = snap;
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();

        // >>> [ADD] 비활성화 시 보유 중인 점유를 모두 반납
        ReleaseAllReservations();
    }

    void OnDestroy()
    {
        // >>> [ADD] 파괴 시에도 안전하게 반납
        ReleaseAllReservations();
    }

    // Input System 이벤트에서 호출
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        // InputSystem 내부에서 4방향 통제 불가로 인한 후처리
        if (moveInput.x != 0f && moveInput.y != 0f) moveInput.x = 0f;
    }

    // 그리드 셀 중앙 좌표 반환 (타일맵 기준, 없으면 1x1 가정)
    Vector2 GetCellCenter(Vector2 worldPos)
    {
        if (collisionTilemap != null)
        {
            Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
            Vector3 c = collisionTilemap.GetCellCenterWorld(cell);
            return new Vector2(c.x, c.y);
        }
        // 타일맵 없으면 격자 1x1 가정
        return new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
    }

    // ===== [ADD] 월드 <-> 셀 변환 유틸 =====
    Vector2Int WorldToCell(Vector2 worldPos)
    {
        if (collisionTilemap == null)
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        Vector3Int c = collisionTilemap.WorldToCell(worldPos);
        return new Vector2Int(c.x, c.y);
    }

    // ===== [ADD] 셀 센터 월드 좌표 유틸 =====
    Vector2 CellCenterWorld(Vector2Int cell)
    {
        if (collisionTilemap == null)
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        Vector3 c = collisionTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        return new Vector2(c.x, c.y);
    }

    // 해당 월드 좌표가 막힌 셀인가?
    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell); // 타일 있으면 '막힘'
    }

    // ===== [ADD] 타일(벽)만 확인하는 한 칸 가능 검사
    // 점유(좀비) 충돌은 "시작 시 TryReserve"로 처리한다
    bool CanStepTileOnly(Vector2 dirUnit)
    {
        Vector2 nextCenter = GetCellCenter(rb.position + dirUnit); // 한 칸 이동 가정
        return !IsBlockedCell(nextCenter);
    }

    // ===== [ADD] 현재 셀 예약 보장(없으면 예약, 바뀌었으면 갱신)
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
                // 이론상 거의 올 일 없음(내가 서 있는 칸이 타인 점유?)
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

    // ===== [ADD] 보유 중인 모든 예약 반납
    void ReleaseAllReservations()
    {
        if (collisionTilemap == null) return;

        if (hasCurrentReservation)
        {
            GridOccupancy.Release(collisionTilemap, currentReservedCell, this);
            hasCurrentReservation = false;
        }
        if (nextReservedCell.HasValue)
        {
            GridOccupancy.Release(collisionTilemap, nextReservedCell.Value, this);
            nextReservedCell = null;
        }
    }
    void changeViewDirection(Vector2 inputDir)
    {
        // input direction 과 currentViewDirection 이 다른 경우 변경을 진행 함.
        if (currentViewDirection != inputDir.normalized)
        {
            currentViewDirection = inputDir.normalized;
        } 
    }

    // 그리드 단위 이동
    void FixedUpdate()
    {
        // 이동 중인 경우
        if (isMoving)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);

            // *** [CHANGE] 타일 막힘은 이동 시작 전에만 검사(여기선 스냅만 처리)
            rb.MovePosition(newPos);

            // 목표 그리드 도착 판정
            if (Vector2.Distance(rb.position, targetPosition) < 0.001f)
            {
                // ===== [ADD] 도착: 이전 셀 반납, 다음 셀 -> 현재 셀로 승계
                if (nextReservedCell.HasValue)
                {
                    if (hasCurrentReservation)
                        GridOccupancy.Release(collisionTilemap, currentReservedCell, this);

                    currentReservedCell = nextReservedCell.Value;
                    hasCurrentReservation = true;
                    nextReservedCell = null;
                }

                // 입력이 있으면 연속 이동 시도(다음 셀 선점 포함)
                if (moveInput.sqrMagnitude > 0.1f)
                {
                    Vector2 dir = moveInput.normalized;
                    dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

                    if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
                    {
                        // >>> [ADD] 다음 셀 계산 + 선점 시도
                        Vector2 curCenter = GetCellCenter(rb.position);
                        Vector2Int nextCell = WorldToCell(curCenter + dir * 1.0f);

                        if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this)) // ★ 선점 핵심
                        {
                            nextReservedCell = nextCell;

                            rb.MovePosition(curCenter); // 스냅 정렬
                            targetPosition = CellCenterWorld(nextCell);
                            currentViewDirection = (targetPosition - rb.position).normalized;
                            isMoving = true; // 계속 이동
                        }
                        else
                        {
                            // >>> [ADD] 좀비 등 점유 중 → 연속 이동 차단
                            isMoving = false;
                            rb.MovePosition(curCenter);
                            targetPosition = curCenter;
                        }
                    }
                    else
                    {
                        isMoving = false;
                        Vector2 snap = GetCellCenter(rb.position);
                        rb.MovePosition(snap);
                        targetPosition = snap;
                    }
                }
                else
                {
                    // 입력 없음 → 멈춤 & 스냅
                    isMoving = false;
                    Vector2 snap = GetCellCenter(rb.position);
                    rb.MovePosition(snap);
                    targetPosition = snap;
                }
            }

            return;
        }

        // ─────────────────────────────────────────────
        // 정지 상태: 첫 한 칸 시작(벽 체크 + 다음 셀 선점)
        // ─────────────────────────────────────────────
        if (moveInput.sqrMagnitude > 0.1f)
        {
            Vector2 dir = moveInput.normalized;
            dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

            if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
            {
                Vector2 curCenter = GetCellCenter(rb.position);
                Vector2Int nextCell = WorldToCell(curCenter + dir * 1.0f);

                // ===== [ADD] ★★★ 다음 칸 선점이 핵심 ★★★
                if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this))
                {
                    nextReservedCell = nextCell;

                    // >>> [ADD] 현재 셀도 반드시 예약되어 있어야 함(좀비가 못 들어오게)
                    EnsureCurrentCellReserved();

                    rb.MovePosition(curCenter);                 // 스냅 정렬
                    isMoving = true;
                    targetPosition = CellCenterWorld(nextCell); // 다음 칸 중앙을 타깃으로
                    
                    changeViewDirection(dir);
                }
                else
                {
                    // >>> [ADD] 좀비가 선점 중 → 시작 자체를 막음
                    Vector2 center = GetCellCenter(rb.position);
                    rb.MovePosition(center);
                    targetPosition = center;

                    changeViewDirection(dir);          // <<< 여기 때문에 "정지 방향 전환" 됨

                    isMoving = false;
                }
            }
            else
            {
                Vector2 curCenter = GetCellCenter(rb.position);
                rb.MovePosition(curCenter);
                targetPosition = curCenter;

                if (dir.sqrMagnitude > 0.1f)
                {
                    changeViewDirection(dir);          // <<< 여기
                }

                isMoving = false;
            }
        }
        else
        {
            // >>> [ADD] 입력이 없어도 현재 셀 예약은 항상 보장
            EnsureCurrentCellReserved();
        }
    }
}
