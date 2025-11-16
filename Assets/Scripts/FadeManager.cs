using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [Header("Fade Image Setting")]
    public Image fadeImage; 
    public float fadeDuration = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void FadeOutToScene(string sceneName)
    {
        StartCoroutine(FadeOutIn(sceneName));
    }

    private IEnumerator FadeOutIn(string sceneName)
    {
        // 화면 어둡게 만들기
        yield return StartCoroutine(Fade(0f, 1f));

        // 씬 전환
        SceneManager.LoadScene(sceneName);

        // 잠깐 대기
        yield return null;

        // 화면 밝게
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float start, float end)
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, end, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = end;
        fadeImage.color = c;
    }
}
