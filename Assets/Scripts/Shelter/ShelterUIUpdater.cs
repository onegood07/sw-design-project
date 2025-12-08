using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShelterUIUpdater : MonoBehaviour
{
    // ⭐ 납입 점수 게이지를 Image 컴포넌트로 변경
    [Header("납입 점수 UI")]
    public Image scoreImage;             // 납입 점수 게이지 (Fill Amount로 채워질 Image)
    public Text scoreText;               // 점수 표시 텍스트 (예: 50 / 100)

    [Header("생존자 정보 UI")]
    public Text lossCountText;           // 예상 사망자 수 표시 텍스트 (스크린샷의 "-5" 부분)
    public Text survivorCountText;       // 현재 생존자 수 표시 텍스트 (스크린샷의 "4" 부분)

    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
            enabled = false;
            return;
        }
        
        // ⭐ Image 컴포넌트의 Type이 Filled이고 Method가 Radial/Horizontal/Vertical 중 하나인지 확인해야 합니다.
        if (scoreImage != null && scoreImage.type != Image.Type.Filled)
        {
             Debug.LogWarning("Score Image의 Type이 Filled가 아닙니다. Fill Amount 속성이 제대로 작동하지 않을 수 있습니다.");
        }
        
        UpdateUI();
    }

    // 매 프레임 또는 납입 이벤트 발생 시 호출
    void Update()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (gm == null) return;
        
        // 1. 납입 점수 업데이트
        int currentScore = gm.GetCurrentSubmittedTotalScore();
        float targetScore = gm.TargetRequiredScore; // 계산을 위해 float으로 변환
        
        // ⭐ Image의 Fill Amount를 설정: (현재 점수 / 목표 점수)
        if (scoreImage != null)
        {
            float fillRatio = currentScore / targetScore;
            scoreImage.fillAmount = Mathf.Clamp01(fillRatio); // 0.0 ~ 1.0 사이 값으로 클램프
        }
        
        // 텍스트 표시
        scoreText.text = $"{currentScore} / {targetScore}";

        // 2. 생존자 정보 업데이트
        int predictedLoss = gm.PredictSurvivorLoss();
        
        // 실제 생존자 수 표시
        survivorCountText.text = $"{gm.SurvivorCount}"; 

        // 예상 손실 인원수 표시
        if (predictedLoss > 0)
        {
            lossCountText.text = $"-{predictedLoss}";
            // lossCountText.color = Color.red; // (선택 사항)
        }
        else
        {
            lossCountText.text = "0";
            // lossCountText.color = Color.green; // (선택 사항)
        }
    }
}