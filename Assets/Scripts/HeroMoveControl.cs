using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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
    void Awake()
    {

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

    // 그리드 단위 이동
    void FixedUpdate()
    {
        // -- 1 이동중인 경우 (그리드 단위 이동이 끝나지 않은 경우) --
        if (isMoving)
        {

            // targetPosition 방향으로 계속 이동
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            // 한 그리드 이동이 끝난 경우
            if (Vector2.Distance(rb.position, targetPosition) < 0.001f)
            {
                // 입력값이 있으면 멈추지 않고 계속 이동한다
                if (moveInput.sqrMagnitude > 0.1f)
                {
                    isMoving = true;
                    targetPosition += moveInput.normalized * 1.0f;
                    currentViewDirection = (targetPosition - rb.position).normalized; // 플레이어 시야 방향. (외부 사용)
                } // 입력값이 없으면 해당 그리드에서 멈춘다.
                else
                {
                    isMoving = false;
                    rb.MovePosition(targetPosition);
                }
            }
        }
        else // -- 2 정지 상태였던 경우
        {
            // 정지 상태에서 입력값이 들어오면 isMoving 및 targetPosition 변경.
            if (moveInput.sqrMagnitude > 0.1f)
            {
                isMoving = true;
                targetPosition = rb.position + moveInput.normalized * 1.0f;
                currentViewDirection = (targetPosition - rb.position).normalized; // 플레이어 시야 방향. (외부 사용)
            }
        }
    }
}
