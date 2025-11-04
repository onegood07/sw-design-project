using UnityEngine;
using UnityEngine.InputSystem;

public class HeroInteraction : MonoBehaviour
{
    public HeroMoveControl heroMove;
    private Vector2 viewDirection;
    private LayerMask interactableLayer; // 컴마로 구분해서 더 넣을 수 있음.

    void Start()
    {
        interactableLayer = LayerMask.GetMask("Interactable");
        heroMove = GetComponent<HeroMoveControl>();
        if (heroMove == null) Debug.LogError("컴포넌트 참조 에러");
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) // 눌렀을 때만 실행
        {
            tryInteraction();
            // Debug.Log("interaction!");
        }    
    }
    void tryInteraction()
    {
        viewDirection = heroMove.CurrentViewDirection;
        rayCast(viewDirection);
    }
    void rayCast(Vector2 viewDirection)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            viewDirection,
            1.0f,
            interactableLayer
        );

        Debug.DrawRay(transform.position, viewDirection * 1.0f, Color.red, 1.0f);
        if (hit.collider != null)
        {
            Debug.Log(hit.collider);
            IInteractable targetObj = hit.collider.GetComponent<IInteractable>();
            if(targetObj != null)
            {
                Debug.Log(targetObj);
                targetObj.OnInteract();
            }
        }
    }
}
