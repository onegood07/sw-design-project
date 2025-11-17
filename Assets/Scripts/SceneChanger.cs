using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "InsideShelter";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 트리거에 진입했습니다. '{nextSceneName}' 씬으로 전환합니다.");
            
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOutToScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("FadeManager가 씬에 없습니다. 바로 씬 전환합니다.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
