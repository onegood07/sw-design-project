using UnityEngine;
using UnityEngine.InputSystem;

public class HeroInteraction : MonoBehaviour
{
    public HeroMoveControl HeroMoveControl;
    private Vector2 viewDirection;
    // 컴마로 구분해서 더 넣을 수 있음.
    // rayCast의 성능 향상을 위해 interactableLayer를 가진 요소만 충돌요소로 본다.
    private LayerMask interactableLayer; 
    

    void Start()
    {
        interactableLayer = LayerMask.GetMask("Interactable");
        HeroMoveControl = GetComponent<HeroMoveControl>();
        if (HeroMoveControl == null) Debug.LogError("컴포넌트 참조 에러");
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) // 눌렀을 때만 실행 (spacebar)
        {
            tryInteraction();
            // Debug.Log("interaction!");
        }    
    }
    void tryInteraction()
    {
        // 시야 방향으로 rayCast 진행
        viewDirection = HeroMoveControl.CurrentViewDirection;
        rayCast(viewDirection);
    }
    void rayCast(Vector2 viewDirection)
    {
        // rayCast 설정
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            viewDirection,
            1.0f,
            interactableLayer
        );

        Debug.DrawRay(transform.position, viewDirection * 1.0f, Color.red, 1.0f);
        if (hit.collider != null)
        {
            // Debug.Log($"{hit.collider} hit");
            // IInteractable 규칙이 있는 오브젝트의 경우, OnInteract 를 실행한다.
            IInteractable targetObj = hit.collider.GetComponent<IInteractable>();
            if(targetObj != null)
            {
                // Debug.Log($"{targetObj} interaction target");
                targetObj.OnInteract();
            }
        }
    }
}
