using UnityEngine;

public class ClickToSkip : MonoBehaviour 
{
    public GameObject skipUIPanel; 

    // 콜라이더가 트리거 영역에 진입했을 때 호출
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 진입한 오브젝트가 Player 태그를 가졌는지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log("Hero가 침대 상호작용 영역에 진입!"); 

            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == Phase.Night)
            {
                Debug.Log("현재 밤 페이즈 - UI 활성화 시도");
                
                if (skipUIPanel != null)
                {
                    skipUIPanel.SetActive(true);
                    Debug.Log("침대 충돌 감지: 밤 스킵 UI 활성화 완료!");
                }
                else
                {
                    Debug.LogError("Skip UI Panel이 ClickToSkip 스크립트에 연결되지 않았습니다!");
                }
            }
            else
            {
                Debug.Log("낮 또는 다른 페이즈이므로 UI를 띄우지 않음!");
            }
        }
    }
}