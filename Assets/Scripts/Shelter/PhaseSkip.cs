using UnityEngine;

public class ClickToSkip : MonoBehaviour 
{
    [Header("UI Reference")]
    public GameObject skipUIPanel; // 침대 근처에 접근 시 표시할 밤 스킵 UI 패널

    // MARK: 플레이어가 침대 영역에 들어왔을 때 호출
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 객체가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            // GameManager가 존재하고 현재 페이즈가 Night인지 확인
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == Phase.Night)
            {
                // UI 패널이 존재하면 활성화
                if (skipUIPanel != null)
                {
                    skipUIPanel.SetActive(true);
                    Debug.Log("침대 영역 진입: 밤 스킵 UI 활성화!");
                }
            }
        }
    }

    // MARK: 플레이어가 침대 영역에서 벗어났을 때 호출
    private void OnTriggerExit2D(Collider2D other)
    {
        // 충돌한 객체가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            // UI가 활성화되어 있으면 비활성화
            if (skipUIPanel != null && skipUIPanel.activeSelf)
            {
                skipUIPanel.SetActive(false);
                Debug.Log("침대 영역 이탈: 밤 스킵 UI 비활성화!");
            }
        }
    }

    // MARK: 플레이어가 UI에서 '스킵하기' 버튼 클릭 시 호출
    public void SkipNightConfirmedAndHideUI()
    {
        // GameManager에서 밤 스킵 로직 실행
        GameManager.Instance?.SkipNightConfirmed(); 
        
        // UI 패널 비활성화 (버튼 클릭으로 스킵했으므로 닫음)
        if (skipUIPanel != null)
        {
            skipUIPanel.SetActive(false);
            Debug.Log("밤 스킵 완료!");
        }
    }

    // MARK: 플레이어가 UI에서 '스킵 거절' 버튼 클릭 시 호출
    public void SkipNightDeclined()
    {
        // UI 패널 비활성화
        if (skipUIPanel != null)
        {
            skipUIPanel.SetActive(false);
            Debug.Log("밤 스킵 거절. 밤 페이즈 지속!");
        }
    }
}
