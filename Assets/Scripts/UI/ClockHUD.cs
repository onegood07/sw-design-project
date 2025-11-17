using UnityEngine;
using UnityEngine.UI;

public class ClockHUD : MonoBehaviour
{
    public float Timer = 0f ;
    public RectTransform ClockHand;
    private Phase prevPhase;

    void Start()
    {
        prevPhase = GameManager.Instance.CurrentPhase;
    }

    void Update()
    {
        Phase curPhase = GameManager.Instance.CurrentPhase;
        if (curPhase != prevPhase)
        {
            Timer = 0f;
            prevPhase = curPhase;
        }

        Timer += Time.deltaTime;

        float maxTime;
        if (GameManager.Instance.CurrentPhase == Phase.Day)
            maxTime = GameManager.Instance.dayDuration;
        else
            maxTime = GameManager.Instance.nightDuration;
        
        float angle = (Timer / maxTime) * 360f;
        ClockHand.pivot = new Vector2(0.5f, 0.2f);
        ClockHand.localRotation = Quaternion.Euler(0, 0, -angle);
    }
}
