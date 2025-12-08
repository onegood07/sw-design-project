using UnityEngine;
using UnityEngine.UI;

public class ShelterHUD : MonoBehaviour
{
    public enum InfoType { SubmittedScore, PredictedLoss }
    public InfoType type;

    // (fillSpeed와 Lerp 관련 변수들 모두 제거)

    Image myImage;
    Text myText;

    void Awake()
    {
        myImage = GetComponent<Image>();
        myText = GetComponent<Text>();
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ShelterHUD] GameManager 인스턴스를 찾을 수 없습니다.");
            enabled = false;
        }
    }

    // Start는 더 이상 필요하지 않지만, Awake/Start 구조를 유지하려면 여기에 둘 수 있습니다.
    // void Start() { } 

    void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        
        switch (type)
        {
            case InfoType.SubmittedScore:
                HandleSubmittedScore(gm);
                break;

            case InfoType.PredictedLoss:
                HandlePredictedLoss(gm);
                break;
        }
    }

    private void HandleSubmittedScore(GameManager gm)
    {
        int currentScore = gm.GetCurrentSubmittedTotalScore();
        float targetScore = gm.TargetRequiredScore;
        
        // 1. 텍스트 업데이트 (즉시 적용)
        if (myText != null)
        {
            myText.text = $"{currentScore} / {targetScore}";
        }

        // 2. ⭐ 막대바 즉시 적용 (Image가 있을 경우)
        if (myImage != null)
        {
            float fillRatio = currentScore / targetScore;
            
            // ⭐ 목표 비율을 즉시 fillAmount에 적용
            myImage.fillAmount = Mathf.Clamp01(fillRatio); 
        }
    }

    private void HandlePredictedLoss(GameManager gm)
    {
        if (myText != null)
        {
            int predictedLoss = gm.PredictSurvivorLoss();
            
            if (predictedLoss > 0)
            {
                myText.text = $"-{predictedLoss}";
                // myText.color = Color.red; // (선택 사항)
            }
            else
            {
                myText.text = "0";
                // myText.color = Color.green; // (선택 사항)
            }
        }
    }
}