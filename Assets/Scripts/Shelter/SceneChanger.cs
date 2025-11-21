using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "InsideShelter"; // 플레이어가 진입하면 이동할 씬 이름

    // MARK: 플레이어가 트리거 영역에 들어왔을 때 호출
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 객체가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 트리거에 진입했습니다. '{nextSceneName}' 씬으로 전환합니다.");
            
            // FadeManager가 존재하면 페이드 효과와 함께 씬 전환
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOutToScene(nextSceneName);
            }
            else
            {
                // FadeManager가 없으면 바로 씬 전환
                Debug.LogWarning("FadeManager가 씬에 없습니다. 바로 씬 전환합니다.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
