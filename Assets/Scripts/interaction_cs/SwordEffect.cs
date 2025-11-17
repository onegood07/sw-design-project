using UnityEngine;

public class SwordEffect : MonoBehaviour
{
    private Animator animator; // 애니메이터 컴포넌트
    private SpriteRenderer spriteRenderer; // 스프라이트 렌더러 컴포넌트

    // 플레이어 공격 스크립트에서 호출될 메서드: 이펙트의 방향을 설정합니다.
    // 4방향에 따라 검기의 이미지 방향(회전)이 달라질 때 사용합니다.
    public void Setup(Vector2 direction)
    {
        if (animator == null || spriteRenderer == null) return;
        
        // 1. 방향에 따른 회전 설정
        // 검기 이미지가 오른쪽을 바라보도록 만들어졌다고 가정합니다.
        if (direction == Vector2.left)
        {
            // 왼쪽을 바라보게 X축으로 뒤집습니다.
            spriteRenderer.flipX = true;
        }
        else if (direction == Vector2.right)
        {
            spriteRenderer.flipX = false;
        }
        else if (direction == Vector2.down)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90f);
        }
        else if (direction == Vector2.up)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90f);
        }
        
        // 2. 애니메이션 길이만큼 후에 파괴
        // Animator의 첫 번째 레이어(0)에 있는 현재 재생 클립 정보를 가져옵니다.
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            float clipLength = clipInfo[0].clip.length;
            // 애니메이션이 끝나면 이 오브젝트를 파괴합니다.
            Destroy(gameObject, clipLength);
            Debug.Log("destroy");
        }
        else
        {
            // 애니메이션 클립을 찾지 못하면 기본 시간 후 파괴 (에러 방지)
            Destroy(gameObject, 0.5f);
            Debug.LogWarning("SwordEffect: 애니메이터 클립을 찾을 수 없습니다. 기본 시간 후 파괴됩니다.");
        }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null) Debug.LogError("SwordEffect 스크립트는 Animator 컴포넌트가 필요합니다.");
        if (spriteRenderer == null) Debug.LogError("SwordEffect 스크립트는 SpriteRenderer 컴포넌트가 필요합니다.");
        
        // **중요**: 검기는 이동하지 않으므로, 충돌 처리를 하려면 여기에 Collider를 추가하거나 확인해야 합니다.
        // GetComponent<Collider2D>().enabled = true; 
    }
    
    // 이펙트는 이동하지 않으므로 Update는 필요 없습니다.

    // 옵션: 검기가 적과 충돌하여 데미지를 주어야 한다면 이 메서드를 사용합니다.
    /*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 데미지 처리 로직
        }
    }
    */
}