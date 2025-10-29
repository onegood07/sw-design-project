using UnityEngine;

public class HeroMoveControl : MonoBehaviour
{
    public float moveSpeed = 5f; // 초기 이동 속도
    private Rigidbody2D rb; // 2D 물리 엔진 컴포넌트 참조
    private Vector2 moveInput;    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 입력 감지 (WASD -> Horizontal/Vertical)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(h, v).normalized;
    }
    
    void FixedUpdate()
    {
        // 물리 이동 처리
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
