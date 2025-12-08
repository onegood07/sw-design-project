using UnityEngine;
using UnityEngine.UI;

public class ClockHUD : MonoBehaviour
{
    // public float Timer = 0f ;       // ⭐ 삭제: GameManager에서 관리
    public RectTransform ClockHand; // Clockhand 객체 변수 생성 (unity)
    // private Phase prevPhase;        // ⭐ 삭제: GameManager가 페이즈 전환을 관리하므로 필요 없음

    void Start()
    {
        // prevPhase = GameManager.Instance.CurrentPhase;  // ⭐ 삭제
    }

    void Update()
    {
        // GameManager 인스턴스가 유효한지 확인
        if (GameManager.Instance == null) return;
        
        float curTimer;
        float maxTime;      

        // 1. 현재 페이즈에 맞는 타이머 값과 최대 시간을 GameManager에서 가져옵니다.
        if (GameManager.Instance.CurrentPhase == Phase.Day)
        {
            curTimer = GameManager.Instance.DayTimer; // ⭐ DayTimer 사용
            maxTime = GameManager.Instance.dayDuration;
        }
        else
        {
            curTimer = GameManager.Instance.NightTimer; // ⭐ NightTimer 사용
            maxTime = GameManager.Instance.nightDuration;
        }
        
        // 2. 비율 계산 및 바늘 회전
        // Clamp01을 사용하여 타이머가 maxTime을 초과하더라도 1.0f를 넘지 않게 보장
        float ratio = Mathf.Clamp01(curTimer / maxTime);
        float angle = ratio * 360f;
        
        // 바늘의 회전 중심점 설정 (pivot은 Inspector에서 0.5f, 0.2f로 설정하는 것이 일반적)
        // ClockHand.pivot = new Vector2(0.5f, 0.2f); // 여기서 강제 설정 대신 Inspector 설정 권장

        // 각도 적용 (시계 방향 회전이 보통 음수)
        ClockHand.localRotation = Quaternion.Euler(0, 0, -angle);
        // 최대 시간 비율에 따라 바늘이 돌아가도록 설정
    }
}