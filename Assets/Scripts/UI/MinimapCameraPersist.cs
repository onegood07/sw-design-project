using UnityEngine;

public class MinimapCameraPersist : MonoBehaviour
{
    private void Awake()
    {
        // 씬 전환 시 삭제되지 않도록 설정
        DontDestroyOnLoad(gameObject);

        // 중복 방지: 이미 존재하는 Minimap Camera가 있으면 삭제
        MinimapCameraPersist[] existingCams = FindObjectsOfType<MinimapCameraPersist>();
        if (existingCams.Length > 1)
        {
            Destroy(gameObject);
        }
    }
}
