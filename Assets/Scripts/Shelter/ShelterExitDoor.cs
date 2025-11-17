using UnityEngine;
using UnityEngine.SceneManagement; 

public class ShelterExitDoor : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "Main";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 쉘터 문에 닿음. '{nextSceneName}' 씬으로 전환합니다.");
            FadeManager.Instance.FadeOutToScene(nextSceneName);
        }
    }
}
