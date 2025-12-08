using System.Collections;
using UnityEngine;

/// <summary>
/// Hero 하위에 있는 Hero_S / Hero_N / Hero_W / Hero_E 이미지를
/// "공격 방향"에 맞게 잠깐 보여주는 스크립트.
/// 
/// - Animator를 잠깐 껐다가 다시 켜면서,
/// - 지정한 시간동안만 해당 방향 이미지를 SetActive(true) 했다가 false 로 돌려놓는다.
/// </summary>
public class HeroAttackFlash : MonoBehaviour
{
    [Header("공격 방향별 Hero 이미지 (Hero 하위 자식)")]
    [SerializeField] private GameObject hero_S;
    [SerializeField] private GameObject hero_N;
    [SerializeField] private GameObject hero_W;
    [SerializeField] private GameObject hero_E;

    [Header("Hero 애니메이터")]
    [SerializeField] private Animator animator;

    [Header("기본 Hero 이미지 (애니메이터가 그려주는 본체 스프라이트)")]
    [SerializeField] private SpriteRenderer baseRenderer;
    private bool baseRendererWasEnabled;

    private void Awake()
    {
        // 시작 시 전부 끈 상태로 시작
        SetAll(false);

        // 인스펙터에서 안 넣어줬다면 자동으로 찾아보기
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (baseRenderer == null)
        {
            baseRenderer = GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// 공격 방향(dir)에 맞는 Hero_* 이미지를 잠깐 보여준다.
    /// dir은 마우스 방향 등 "공격하려는 방향" 벡터.
    /// </summary>
    public void ShowAttack(Vector2 dir, float duration = 0.1f)
    {
        // 0벡터면 아무 것도 하지 않음
        if (dir.sqrMagnitude < 0.0001f)
            return;

        StartCoroutine(ShowAttackRoutine(dir, duration));
    }

    private IEnumerator ShowAttackRoutine(Vector2 dir, float duration)
    {
        // 기본 Hero 스프라이트 잠깐 끄기 (현재 애니메이션 프레임 숨기기)
        if (baseRenderer != null)
        {
            baseRendererWasEnabled = baseRenderer.enabled;
            baseRenderer.enabled = false;
        }

        // 애니메이터 잠깐 끄기 (애니메이션 진행 정지)
        if (animator != null)
            animator.enabled = false;

        // 전부 끄고 시작
        SetAll(false);

        // 방향 스냅: 더 큰 축 기준으로 4방향 중 하나 선택
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x < 0)
            {
                if (hero_W != null) hero_W.SetActive(true);
            }
            else
            {
                if (hero_E != null) hero_E.SetActive(true);
            }
        }
        else
        {
            if (dir.y < 0)
            {
                if (hero_S != null) hero_S.SetActive(true);
            }
            else
            {
                if (hero_N != null) hero_N.SetActive(true);
            }
        }

        // 잠깐 보여주기
        yield return new WaitForSeconds(duration);

        // 다시 끄기 (공격 방향용 Hero_* 이미지 비활성화)
        SetAll(false);

        // 기본 Hero 스프라이트 다시 보이기
        if (baseRenderer != null)
        {
            baseRenderer.enabled = baseRendererWasEnabled;
        }

        // 애니메이터 다시 켜기 (원래 애니메이션 상태 재개)
        if (animator != null)
            animator.enabled = true;
    }

    private void SetAll(bool visible)
    {
        if (hero_S != null) hero_S.SetActive(visible);
        if (hero_N != null) hero_N.SetActive(visible);
        if (hero_W != null) hero_W.SetActive(visible);
        if (hero_E != null) hero_E.SetActive(visible);
    }
}


