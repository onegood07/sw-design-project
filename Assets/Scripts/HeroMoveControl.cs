using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
public class HeroMoveControl : MonoBehaviour
{
    private Vector2 currentViewDirection = new Vector2(0f,-1f); // 시야 방향. 외부 참조 변수
    public Vector2 CurrentViewDirection
    {
        get { return currentViewDirection; }
    }
    public float moveSpeed = 5f; // 초기 이동 속도
    private Rigidbody2D rb; // 2D 물리 엔진 컴포넌트 참조
    private Vector2 targetPosition; // 이동 위치
    // Input Actions Asset을 public으로 참조
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private Vector2 moveInput;
    private bool isMoving = false; // 그리드 단위 이동 변수. true 일 경우 입력을 무시하고 1그리드 이동
    [SerializeField]
    private Vector2 initHeroPosition = new Vector2(0.5f, 0.5f); // Hero 초기화 좌표.
    [SerializeField] Tilemap collisionTilemap;
    void Awake()
    {
        // 충돌맵 찾아서 넣어주기
        collisionTilemap = GameObject.Find("collision").GetComponent<Tilemap>();
        // Action Map에서 Move 액션 가져오기
        moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");

        if (moveAction != null) // Aciton 맵 있는지 확인
            moveAction.Enable(); // 입력 활성화
        else
            Debug.LogError("Move 액션을 찾을 수 없습니다");
        // Move 액션이 에셋에 없을 경우 콘솔에 에러 메시지 출력
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.MovePosition(initHeroPosition); // 그리드 내부로 Hero 위치 초기화 
    }


    void OnDisable()
    {
        // 이벤트 중복 호출 방지
        if (moveAction != null)
            moveAction.Disable(); // 입력 액션을 비활성화
    }

    // Input System 이벤트에서 호출
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        // InputSystem 내부에서 4방향 통제 불가로 인한 후처리
        if (moveInput.x != 0f && moveInput.y != 0f) moveInput.x = 0f;
    }
    // void FixedUpdate()
    // {
    //     // 물리 이동 처리
    //     if (moveAction != null)
    //     {
    //         Vector2 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
    //         rb.MovePosition(newPos);
    //     }
    // }
    
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

    // 해당 월드 좌표가 막힌 셀인가?
    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell); // 타일 있으면 '막힘'
    }

    // 다음 한 칸(step)으로 갈 수 있는가? (셀 중앙 기준)
    bool CanStep(Vector2 dirUnit)
    {
        Vector2 nextCenter = GetCellCenter(rb.position + dirUnit); // 한 칸(그리드 1) 이동 가정
        return !IsBlockedCell(nextCenter);
    }

    // 그리드 단위 이동
    void FixedUpdate()
    {
        if (isMoving)
        {
            // 이번 프레임에 이동하려는 후보 위치 계산
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);

            // 이동위치가 들어갈 셀 중앙을 기준으로 막힘 검사
            Vector2 nextCenter = GetCellCenter(newPos);
            if (IsBlockedCell(nextCenter))
            {
                // 막혀 있으면 이번 프레임은 이동하지 않음 
                isMoving = false;
                // 현재 셀 중앙으로만 정렬
                Vector2 snap = GetCellCenter(rb.position);
                rb.MovePosition(snap);
                targetPosition = snap;
                return;
            }

            // 실제 이동
            rb.MovePosition(newPos);

            // 목표 그리드 도착 판정
            if (Vector2.Distance(rb.position, targetPosition) < 0.001f)
            {
                // 입력값이 있으면 멈추지 않고 계속 이동한다
                if (moveInput.sqrMagnitude > 0.1f)
                {
                    // 상하좌우 스냅
                    Vector2 dir = moveInput.normalized;
                    dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

                    //다음 칸 시작 전 충돌 검사 (셀 중앙 기준)
                    if (dir.sqrMagnitude > 0.1f && CanStep(dir))
                    {
                        isMoving = true;
                        // 현재 위치를 셀 중앙으로 맞춘 뒤 다음 칸 중앙을 타깃으로
                        Vector2 curCenter = GetCellCenter(rb.position);
                        rb.MovePosition(curCenter);
                        targetPosition = curCenter + dir * 1.0f;
                        currentViewDirection = (targetPosition - rb.position).normalized;
                    }
                    else
                    {
                        isMoving = false; // 막히면 멈춤
                    }
                }
                else
                {
                    isMoving = false; // 입력 없음 → 멈춤
                    // 도착 지점 셀 중앙 정렬
                    Vector2 snap = GetCellCenter(targetPosition);
                    rb.MovePosition(snap);
                    targetPosition = snap;
                }
            }

            return;
        }

        // ─────────────────────────────────────────────
        // 정지 상태: 첫 한 칸 시작도 '미리' 차단
        // ─────────────────────────────────────────────
        if (moveInput.sqrMagnitude > 0.1f)
        {
            // 상하좌우 스냅
            Vector2 dir = moveInput.normalized;
            dir = new Vector2(Mathf.Round(dir.x), Mathf.Round(dir.y));

            // ★추가: 시작 전 충돌 검사 (다음 셀 중앙 진입 가능?)
            if (dir.sqrMagnitude > 0.1f && CanStep(dir))
            {
                // 현재를 셀 중앙으로 정렬하고 거기서 한 칸 이동 시작
                Vector2 curCenter = GetCellCenter(rb.position);
                rb.MovePosition(curCenter);

                isMoving = true;
                targetPosition = curCenter + dir * 1.0f;
                currentViewDirection = (targetPosition - rb.position).normalized;
            }
            else
            {
                // 막혀 있으면 시작 자체를 안 함 (아예 못 들어감)
                isMoving = false;
                // 선택사항: 현재도 셀 중앙으로 정렬
                Vector2 snap = GetCellCenter(rb.position);
                rb.MovePosition(snap);
                targetPosition = snap;
            }
        }
    }
}
