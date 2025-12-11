using UnityEngine;

public class CanvasPersist : MonoBehaviour
{
    public static CanvasPersist Instance;
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
