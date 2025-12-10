using UnityEngine;

public class GameOverCanvasManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스 변수 선언
    public static GameOverCanvasManager Instance { get; private set; }

    private void Awake()
    {
        // 🚨 2. 중복 검사: 이미 인스턴스가 존재하면 새로 생성된 자신을 파괴
        if (Instance != null && Instance != this)
        {
            // 이 씬에서 새로 생성된 중복 오브젝트라면 파괴합니다.
            Destroy(gameObject); 
            return;
        }

        // 3. 인스턴스 설정 및 씬 전환 시 파괴 방지
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 처음에는 비활성화 상태로 유지
        gameObject.SetActive(false); 
    }
    
    // 이 함수를 외부에서 호출하여 활성화/비활성화 할 수 있습니다.
    public void SetPanelActive(bool isActive)
    {
        // 1. Canvas 오브젝트 자체를 활성화/비활성화
        // (GameOverCanvasManager가 붙어있는 오브젝트)
        gameObject.SetActive(isActive);
        
        // 2. 활성화 요청(true)인 경우, 혹시 모를 비활성화된 자식 오브젝트(실제 패널 UI)를 강제로 활성화
        if (isActive)
        {
            // Canvas 아래에 있는 모든 자식 오브젝트(게임 오버 패널)를 활성화
            // (자식이 많지 않다면 효율상 큰 문제 없습니다.)
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && !child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                    Debug.Log($"[GameOverCanvasManager] 자식 오브젝트 강제 활성화: {child.name}");
                }
            }
        }
    }
}