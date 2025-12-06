using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HeroInteraction : MonoBehaviour
{
    public HeroMoveControl HeroMoveControl;
    private Vector2 viewDirection;
    // 컴마로 구분해서 더 넣을 수 있음.
    // rayCast의 성능 향상을 위해 interactableLayer를 가진 요소만 충돌요소로 본다.
    private LayerMask interactableLayer; 
    public GameObject swordEffectPrefab;
    public float attackCoolTime;
    // private float lastAttackTime = -1f; 쿨타임용 변수. 좀비 상호작용 제거로 불필요하나 일단 보류
    private Vector2 mouseDirection;

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

            if (IsPointerOverUIObject())
            {
                return;
            }            
            Vector3 mouseWorldPosition3D = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mouseWorldPosition = new Vector2(mouseWorldPosition3D.x, mouseWorldPosition3D.y);

            mouseDirection = mouseWorldPosition - (Vector2)transform.position;
            tryInteraction(mouseDirection);
            // Debug.Log("interaction!");
        }    
    }
    void tryInteraction(Vector2 mouseDirection)
    {
        // 시야 방향으로 rayCast 진행
        rayCast(mouseDirection);
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
                // 좀비 인터렉션 없는 상태. 나중에 근접무기 추가하게 된다면 사용
                // if (hit.collider.CompareTag("Zombie"))
                // {
                //     if (Time.time < lastAttackTime + attackCoolTime)return;
                //     lastAttackTime = Time.time;
                //     createEffect(viewDirection);
                //     targetObj.OnInteract();
                // }
                targetObj.OnInteract();
                // Debug.Log($"{targetObj} interaction target");
            }
        }
    }
    // 좀비 이펙트. 총알 이펙트 사용시 참고
    // void createEffect(Vector2 viewDirection)
    // {
    //     Vector2 createPosition = transform.position + (Vector3)viewDirection.normalized;
    //     GameObject effect = Instantiate(swordEffectPrefab,createPosition,quaternion.identity);
    //     SwordEffect effectSetup = effect.GetComponent<SwordEffect>();
    //     if(effect != null)
    //     {
    //         viewDirection = HeroMoveControl.CurrentViewDirection;
    //         effectSetup.Setup(viewDirection);
    //     }
    // }
    private bool IsPointerOverUIObject()
    {
        // 1. EventSystem 및 PointerData 생성
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        
        // 2. 현재 마우스 위치를 EventData에 설정
        eventData.position = Mouse.current.position.ReadValue();
        
        // 3. Raycast 결과를 담을 리스트
        List<RaycastResult> results = new List<RaycastResult>();
        
        // 4. UI Raycast 실행 (현재 마우스 위치에 UI 요소가 있는지 검사)
        EventSystem.current.RaycastAll(eventData, results);
        
        // results 리스트에 무언가 있다면 (UI 요소가 있다면) true 반환
        return results.Count > 0;
    }
}
