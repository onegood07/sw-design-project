using UnityEngine;

public class MinimapCameraPersist : MonoBehaviour
{
    public static MinimapCameraPersist Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
