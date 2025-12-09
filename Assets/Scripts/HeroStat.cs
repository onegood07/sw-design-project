using UnityEngine;
using System.Collections;

public class HeroStat : MonoBehaviour 
{ 
    public static HeroStat Instance { get; private set; }

    // 생명력
    public float hp;
    public float baseHp = 1000f;
    public float maxHp = 1000f;

    // 허기
    public float hunger;
    public float baseHunger = 1000f;
    public float maxHunger = 1000f;

    // 이동속도
    public float speed = 1000f;
    public float baseSpeed = 1000f;
    // 신발/버프 등으로 인한 일시적 이동속도 증가 여부
    private bool isSpeedBoosted = false;
    public bool isSurvival;

    // 부스트 코루틴 활성 여부
    private Coroutine activeBoostCoroutine;
    [Header("Hit Effect")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Color hitColor = Color.red;
    [SerializeField] float hitEffectDuration = 0.3f;
    Color originalColor;
    Coroutine hitRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

     void Start()
    {
        hp = 1000;
        speed = 1000;
        hunger = 1000;
        isSurvival = true;

        // 일정 시간마다 허기 감소 로직
        StartCoroutine(HungerDecreaseCoroutine());
    }

    void SpeedControl()
    {
        // 허기가 많이 떨어지면 이동 속도 패널티 적용
        if (hunger <= 100f)
        {
            // 너무 빠르면 100으로 제한
            if (speed > 100f)
                speed = 100f;
        }
        else
        {
            // 허기가 충분할 때는 현재 속도가 기본 속도보다 느리면 기본 속도로 복원
            if (speed < baseSpeed)
            {
                speed = baseSpeed;
            }
        }
    }

    // HP 회복 로직
    public void Heal(float amount)
    {
        if (!isSurvival) return;

        hp += amount;
        
        // Max HP를 초과하지 않도록 보정
        if (hp > maxHp)
        {
            hp = maxHp;
        }
        Debug.Log($"HP 회복: +{amount}. 현재 HP: {hp}/{maxHp}");
    }

    // 생명력 감소 로직
    public void decreaseHp(float zombiePower)
    {
        // 좀비의 공격력(Power)만큼 생명력 감소
        hp -= zombiePower;
        PlayHitEffect();

        if (hp <= 0 && isSurvival) 
        {
            hp = 0; // HP를 0으로 고정
            isSurvival = false;
            Debug.Log("플레이어 사망!");
            // 플레이어 사망 시에 GameManager의 플레이어 사망 로직 불러오기
            // GameManager가 싱글톤임을 가정합니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
        }
    }

    void Update()
    {
        SpeedControl();
    }
    /*
    - HeroSpeedBoostCoroutine
    - 인자 : 진행 시간, 상향 혹은 하향 퍼센티지
    - 반환 값 : 없음.
    */
    IEnumerator HeroSpeedBoostCoroutine(float duration, float percentage)
    {
        var mul = baseSpeed*percentage/100;
        if(speed+mul > baseSpeed * 2)
        {
            yield break;
        }
        isSpeedBoosted = true;
        speed += mul;
        Debug.Log("이동속도 증가");

        // 지정 시간 동안 지속
        yield return new WaitForSeconds(duration);

        speed -= mul;
        Debug.Log("돌아옴");
        isSpeedBoosted = false;
    }
    /*
    - ActiveHeroSpeedBoost
    - 인자 : 진행 시간, 상향 혹은 하향 퍼센티지
    - 반환 값 : 없음
    */
    public void ActiveHeroSpeedBoost(float duration, float percentage)
    {
        // 작동 중이었다면
        if(activeBoostCoroutine != null)
        {
            // 코루틴을 중지하고
            StopCoroutine(activeBoostCoroutine);
        }
        // 다시 시작하거나, 첫 시작을 함.
        activeBoostCoroutine = StartCoroutine(HeroSpeedBoostCoroutine(duration,percentage));
    }

    /// <summary>
    /// 장비(신발 등) 해제 시, 남아 있는 이동속도 버프를 즉시 제거한다.
    /// </summary>
    public void CancelEquipSpeedBoost()
    {
        if (activeBoostCoroutine != null)
        {
            StopCoroutine(activeBoostCoroutine);
            activeBoostCoroutine = null;
        }

        isSpeedBoosted = false;

        // 허기 상태에 따라 적절한 기본 속도로 복구
        if (hunger <= 100f)
        {
            speed = 100f;
        }
        else
        {
            speed = baseSpeed;
        }
    }

    void PlayHitEffect()
    {
        if (spriteRenderer == null)
            return;

        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitEffectRoutine());
    }

    IEnumerator HitEffectRoutine()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitEffectDuration);
        spriteRenderer.color = originalColor;
        hitRoutine = null;
    }

    // 일정 시간마다 허기 감소 코루틴
    IEnumerator HungerDecreaseCoroutine()
    {
        while (isSurvival)
        {
            // 1초마다 허기가 10씩 감소
            yield return new WaitForSeconds(1f);
            hunger -= 10f;
            // Debug.Log(hunger);
            // 허기가 음수가 되지 않도록 보정
            if (hunger < 0) hunger = 0;

            // TODO: 허기가 0이 되면 플레이어 피해 처리 (추후 구현)
            if (hunger <= 0)
            {
                // decreaseHp(10f); 
            }
        }
    }
}