using UnityEngine;

public class CanvasPersist : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject); 
    }
}
