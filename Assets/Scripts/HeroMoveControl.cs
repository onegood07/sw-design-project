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
            moveAcion.Enable(); // 입력 활성화
        else
            Debug.LogError("Move 액션을 찾을 수 없습니다");
            // Move 액션이 에셋에 없을 경우 콘솔에 에러 메시지 출력
    }
    
    void FixedUpdate()
    {
        // 물리 이동 처리
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
