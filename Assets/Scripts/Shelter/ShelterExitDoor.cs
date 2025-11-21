using UnityEngine;
using UnityEngine.SceneManagement; 

public class ShelterExitDoor : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "Main"; // 플레이어가 문을 통과하면 이동할 씬 이름

    // MARK: 플레이어가 쉘터 출구 트리거에 들어왔을 때 호출 
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 객체가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 쉘터 문에 닿음. '{nextSceneName}' 씬으로 전환합니다.");

            // FadeManager를 사용하여 페이드 아웃 후 씬 전환
            FadeManager.Instance.FadeOutToScene(nextSceneName);
        }
    }
}
