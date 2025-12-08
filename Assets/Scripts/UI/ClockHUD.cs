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

    // ClockHUD.cs

void Update()
{
    if (GameManager.Instance == null) return;
    
    float curTimer;
    float maxTime;      

    // ⭐ 1. 현재 일차의 동적 최대 시간을 가져옵니다.
    (float dynamicDayTime, float dynamicNightTime) = 
        GameManager.Instance.GetDurationForDay(GameManager.Instance.CurrentDay);
    
    // 2. 현재 페이즈에 맞는 타이머 값과 최대 시간을 설정합니다.
    if (GameManager.Instance.CurrentPhase == Phase.Day)
    {
        curTimer = GameManager.Instance.DayTimer;
        maxTime = dynamicDayTime; // ⭐ 동적으로 가져온 낮 시간을 사용
    }
    else
    {
        curTimer = GameManager.Instance.NightTimer;
        maxTime = dynamicNightTime; // ⭐ 동적으로 가져온 밤 시간을 사용
    }
    
    // 최대 시간이 0이면 오류 방지
    if (maxTime <= 0f) return; 
    
    // 3. 비율 계산 및 바늘 회전
    float ratio = Mathf.Clamp01(curTimer / maxTime);
    float angle = ratio * 360f;
    
    // 각도 적용 (시계 방향 회전이 보통 음수)
    ClockHand.localRotation = Quaternion.Euler(0, 0, -angle);
}
}