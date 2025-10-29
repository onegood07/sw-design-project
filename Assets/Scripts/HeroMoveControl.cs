using UnityEngine;
using UnityEngine.InputSystem;
public class HeroMoveControl : MonoBehaviour
{
    public float moveSpeed = 5f; // 초기 이동 속도
    private Rigidbody2D rb; // 2D 물리 엔진 컴포넌트 참조
    private Vector2 moveInput;

    // Input Actions Asset을 public으로 참조
    public InputActionAsset inputActions;
    private InputAction moveAction;

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
    }
    void FixedUpdate()
    {
        // 물리 이동 처리
        if (moveAction != null)
        {
            Vector2 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }
}
