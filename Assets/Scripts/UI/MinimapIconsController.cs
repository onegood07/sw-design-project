using UnityEngine;
using UnityEngine.SceneManagement;

public class MinimapIconsController : MonoBehaviour
{
    public GameObject minimapIconsGroup;

    private void OnEnable()
    {
        // 씬 전환될 때 자동 호출되도록 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때 자동 실행
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateIconVisibility(scene.name);
    }

    // 초기씬에서도 적용되도록 Start에서 실행
    private void Start()
    {
        UpdateIconVisibility(SceneManager.GetActiveScene().name);
    }

    // 씬 이름 기반으로 아이콘 표시 여부 결정
    private void UpdateIconVisibility(string sceneName)
    {
        // 예: scene 이름에 "Shelter"가 포함되면 내부라고 판단
        if (sceneName.Contains("Main"))
        {
            minimapIconsGroup.SetActive(true);
        }
        else
        {
            minimapIconsGroup.SetActive(false);
        }
    }
}
