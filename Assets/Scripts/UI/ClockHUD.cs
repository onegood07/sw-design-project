using UnityEngine;
using UnityEngine.UI;

public class ClockHUD : MonoBehaviour
{
    public float Timer = 0f ;       // 타이머 초기값 정의
    public RectTransform ClockHand; // Clockhand 객체 변수 생성 (unity)
    private Phase prevPhase;        // 이전 페이즈 저장 변수

    void Start()
    {
        prevPhase = GameManager.Instance.CurrentPhase;  // 시작 페이즈 저장
    }

    void Update()
    {
        Phase curPhase = GameManager.Instance.CurrentPhase; // 게임매니저에서 페이즈 정보 받아오기
        if (curPhase != prevPhase)
        {
            Timer = 0f;
            prevPhase = curPhase;
        }       // 페이즈 변동이 있을 경우 Timer 값을 0으로 초기화

        Timer += Time.deltaTime;    // Timer 시간 경과 업데이트

        float maxTime;      // 최대 시간을 게임 매니저의 페이즈에 따른 시간 값으로 설정
        if (GameManager.Instance.CurrentPhase == Phase.Day)
            maxTime = GameManager.Instance.dayDuration;
        else
            maxTime = GameManager.Instance.nightDuration;
        
        float angle = (Timer / maxTime) * 360f;
        ClockHand.pivot = new Vector2(0.5f, 0.2f);
        ClockHand.localRotation = Quaternion.Euler(0, 0, -angle);
        // 최대 시간 비율에 따라 바늘이 돌아가도록 설정
    }
}
