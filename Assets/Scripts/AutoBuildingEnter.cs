using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChanger : MonoBehaviour
{
    [Header("다음 씬의 이름")]
    public string nextSceneName = "InsideShelter"; // 스크린샷에서 씬 이름이 InsideShelter인 것 같아 예시로 넣었습니다.

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"OnTriggerEnter2D 호출됨. 닿은 태그: {other.tag}");
        // 진입한 오브젝트의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 트리거에 진입했습니다. '{nextSceneName}' 씬으로 전환합니다.");
            
            // 지정된 씬 이름으로 씬을 로드합니다.
            SceneManager.LoadScene(nextSceneName);
        }
    }
}