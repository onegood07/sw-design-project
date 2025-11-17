using UnityEngine;

public class ClickToSkip : MonoBehaviour 
{

    [Header("UI Reference")]
    public GameObject skipUIPanel; 

    // 침대에 가까이 갔을 때 호출
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == Phase.Night)
            {
                if (skipUIPanel != null)
                {
                    skipUIPanel.SetActive(true);
                    Debug.Log("침대 영역 진입: 밤 스킵 UI 활성화!");
                }
            }
        }
    }

    // 침대에서 멀어졌을 때 호출
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 낮이든 밤이든 플레이어가 영역을 벗어나면 UI는 비활성화
            if (skipUIPanel != null && skipUIPanel.activeSelf)
            {
                skipUIPanel.SetActive(false);
                Debug.Log("침대 영역 이탈: 밤 스킵 UI 비활성화!");
            }
        }
    }

    public void SkipNightConfirmedAndHideUI()
    {
        // GameManager에서 밤 스킵 로직 실행
        GameManager.Instance?.SkipNightConfirmed(); 
        
        // UI 비활성화 (버튼 클릭으로 스킵했으므로 무조건 닫음)
        if (skipUIPanel != null)
        {
            skipUIPanel.SetActive(false);
            Debug.Log("밤 스킵 완료!");
        }
    }
    public void SkipNightDeclined()
    {
        if (skipUIPanel != null)
        {
            skipUIPanel.SetActive(false);
            Debug.Log("밤 스킵 거절. 밤 페이즈 지속!");
        }
    }
}