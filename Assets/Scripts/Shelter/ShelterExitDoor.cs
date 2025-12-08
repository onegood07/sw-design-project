using UnityEngine;
using UnityEngine.SceneManagement; 

public class ShelterExitDoor : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "Main"; // 플레이어가 문을 통과하면 이동할 씬 이름

    // ⭐ 플레이어가 메인 씬으로 돌아갈 때 배치될 월드 좌표 (Inspector에서 설정)
    [Header("Main Scene Spawn Point")]
    public Vector3 targetSpawnPosition = new Vector3(0f, -5f, 0f); // 원하는 좌표로 설정

    // MARK: 플레이어가 쉘터 출구 트리거에 들어왔을 때 호출 
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 객체가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 쉘터 문에 닿음. '{nextSceneName}' 씬으로 전환합니다. 목표 위치: {targetSpawnPosition}");

            // GameManager의 헬퍼 함수를 사용하여 위치를 저장하고 씬 전환 요청
            if (GameManager.Instance != null)
            {
                // GameManager가 이 위치를 FadeManager에 전달합니다.
                GameManager.Instance.SetPlayerSpawnAndLoadScene(nextSceneName, targetSpawnPosition);
            }
            else
            {
                Debug.LogError("[ShelterExitDoor] GameManager 인스턴스를 찾을 수 없습니다. 씬 전환만 실행합니다.");
                // FadeManager가 없거나 인스턴스가 없을 경우를 대비한 대체 로직 (권장)
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}