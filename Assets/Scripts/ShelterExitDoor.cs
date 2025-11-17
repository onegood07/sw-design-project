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
            Debug.Log($"플레이어 쉘터 퇴장 : '{nextSceneName}' 씬으로 전환");
            FadeManager.Instance.FadeOutToScene(nextSceneName);
        }
    }
}
