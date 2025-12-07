using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuScript : MonoBehaviour
{
    public void LoadGameScene()
    {
        SceneManager.LoadScene("Main");
    }
}
