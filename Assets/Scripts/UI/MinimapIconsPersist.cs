using UnityEngine;

public class MinimapIconsPersist : MonoBehaviour
{
    public static MinimapIconsPersist Instance;
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
