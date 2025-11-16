using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChanger : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "InsideShelter";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"OnTriggerEnter2D 호출됨. 닿은 태그: {other.tag}");
        // 진입한 오브젝트가 Player인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 트리거에 진입했습니다. '{nextSceneName}' 씬으로 전환합니다.");
            
            // 지정된 씬 이름으로 화면 전환
            FadeManager.Instance.FadeOutToScene(nextSceneName);
        }
    }
}