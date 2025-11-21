using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement; 

public class HeroMoveControl : MonoBehaviour
{
    public static HeroMoveControl Instance { get; private set; }

    private Vector2 currentViewDirection = new Vector2(0f, -1f);
    public Vector2 CurrentViewDirection => currentViewDirection;

    [SerializeField] private float stepTime = 0.4f;
    // 이동속도 heroStat 에서 가져와야 함. 원래는 2.5f였음
    private float moveSpeed;
    private const float minMoveSpeed = 0.5f;
    // maxMoveSpeed 수정 기존 값 3f 였음.
    private const float maxMoveSpeed = 6f;

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
        if (Instance == null)
        {
            Instance = this;
            // 씬이 전환되어도 오브젝트를 파괴하지 않음
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (moveAction != null) moveAction.Disable();
        ReleaseReservation();
    }

    void OnDestroy()
    {
        ReleaseReservation();
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Hero] 씬 로드 완료: {scene.name}");
        
        // 새로운 씬에서 "collision" 타일맵 오브젝트를 찾습니다.
        var colGo = GameObject.Find("collision");
        if (colGo != null)
        {
            collisionTilemap = colGo.GetComponent<Tilemap>();
            Debug.Log($"[Hero] 새로운 collision Tilemap 연결됨.");
        }
        else
        {
            collisionTilemap = null;
            Debug.LogWarning($"[Hero] 씬 '{scene.name}'에서 'collision' Tilemap을 찾을 수 없습니다. (충돌/점유 체크 비활성화)");
        }

        if (rb != null)
        {
            // 캐릭터의 현재 위치를 새 타일맵 그리드 중앙으로 스냅
            Vector2 snap = GetCellCenter(rb.position);
            rb.MovePosition(snap);
            targetPosition = snap;
            
            // collisionTilemap이 null이면 내부에서 null 체크를 통해 예약 로직이 건너뛰어짐
            EnsureCurrentCellReserved(); 
        }
        
        UpdateAnimation(false);
        isMoving = false;
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
        // Tilemap이 없어도 그리드 단위 이동을 위해 0.5f 그리드 중앙으로 스냅 로직 유지
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
        // IsBlockedCell이 null 체크를 포함하므로 이 함수는 항상 안전함
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
        bool attemptReservation = collisionTilemap != null;

        // 수정. stepTime 에 관계 없이 heroStat의 속도 배율만큼 증가함.
        moveSpeed = 1f / stepTime * GetComponent<HeroStat>().speed/1000f;
        moveSpeed = Mathf.Clamp(moveSpeed, minMoveSpeed, maxMoveSpeed);

        // ---------- 이동 중 ----------
        if (isMoving)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            if (Vector2.Distance(rb.position, targetPosition) < 0.001f)
            {
                // 목표 위치 도달 후 스냅
                rb.MovePosition(targetPosition); 

                // 연속 입력
                if (moveInput.sqrMagnitude > 0.1f)
                {
                    Vector2 dir = moveInput.normalized;
                    dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

                    // Tilemap이 없으면 CanStepTileOnly는 항상 true (IsBlockedCell에서 처리)
                    if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
                    {
                        Vector2 curCenter = CellCenterWorld(currentReservedCell);
                        Vector2Int nextCell = WorldToCell(curCenter + dir);

                        bool reservedSuccessfully = !attemptReservation; // Tilemap이 없으면 예약 없이 성공

                        if (attemptReservation) // Tilemap이 있을 때만 Grid Occupancy 실행
                        {
                            if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this))
                            {
                                if (hasCurrentReservation)
                                    GridOccupancy.Release(collisionTilemap, currentReservedCell, this);

                                currentReservedCell = nextCell;
                                hasCurrentReservation = true;
                                reservedSuccessfully = true;
                            }
                            else
                            {
                                reservedSuccessfully = false;
                            }
                        }
                        else
                        {
                            // Tilemap이 없을 때, 혹시 모를 이전 씬의 예약을 해제
                            if (hasCurrentReservation)
                                ReleaseReservation();
                        }


                        if (reservedSuccessfully)
                        {
                            // 이동 시작
                            targetPosition = CellCenterWorld(nextCell);
                            currentViewDirection = dir.normalized;

                            lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                            isMoving = true;
                            UpdateAnimation(true);  // 계속 걷기
                        }
                        else
                        {
                            // Grid Mode에서 점유 실패 → 방향만 바꾸고 멈춤
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
                        // 벽 등으로 더 못감 (Grid Mode에서만 발생) → 방향만 돌고 멈춤
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

            // Tilemap이 없으면 CanStepTileOnly는 항상 true (IsBlockedCell에서 처리)
            if (dir.sqrMagnitude > 0.1f && CanStepTileOnly(dir))
            {
                Vector2 curCenter = GetCellCenter(rb.position);
                Vector2Int nextCell = WorldToCell(curCenter + dir);

                bool reservedSuccessfully = !attemptReservation; // Tilemap이 없으면 예약 없이 성공

                if (attemptReservation) // Tilemap이 있을 때만 Grid Occupancy 실행
                {
                    if (GridOccupancy.TryReserve(collisionTilemap, nextCell, this))
                    {
                        if (hasCurrentReservation)
                            GridOccupancy.Release(collisionTilemap, currentReservedCell, this);

                        currentReservedCell = nextCell;
                        hasCurrentReservation = true;
                        reservedSuccessfully = true;
                    }
                    else
                    {
                        reservedSuccessfully = false;
                    }
                }
                else
                {
                    // Tilemap이 없을 때, 혹시 모를 이전 씬의 예약을 해제
                    if (hasCurrentReservation)
                        ReleaseReservation();
                }

                if (reservedSuccessfully)
                {
                    // 이동 시작
                    rb.MovePosition(curCenter);
                    targetPosition = CellCenterWorld(nextCell);

                    lastMoveDir = new Vector2Int((int)dir.x, (int)dir.y);
                    changeViewDirection(dir);

                    isMoving = true;
                    UpdateAnimation(true); // 걷기 시작
                }
                else
                {
                    // Grid Mode에서 점유 실패 → 방향만 돌리기
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
                // 벽 때문에 못 움직임 (Grid Mode에서만 발생) → 방향만 돌리기
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
    public void SetTargetPosition(Vector2 pos)
    {
        targetPosition = pos;
        rb.MovePosition(pos);
        EnsureCurrentCellReserved();
        isMoving = false;
        UpdateAnimation(false);
    }
    public void ForceMove(Vector2 newPos)
    {
        ReleaseReservation();

        transform.position = newPos;
        targetPosition = newPos;

        isMoving = false;
        UpdateAnimation(false);
        
        EnsureCurrentCellReserved();
    }

    public void SetCollisionTilemap(Tilemap tilemap)
    {
        collisionTilemap = tilemap;
    }

    public void ExitShelter(Vector3 outsidePos)
    {
        ForceMove(outsidePos);
    }

}