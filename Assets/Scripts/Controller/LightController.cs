using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightController : MonoBehaviour
{
    public static LightController Instance;

    [Header("Global Light Settings")]
    public Light2D globalLight;
    public float transitionDuration = 2f;

    [Header("Day & Night")]
    public float dayIntensity = 1.0f;
    public Color dayColor = Color.white;

    public float nightIntensity = 0.2f;
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f);

    private Coroutine lightCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (globalLight == null)
        {
            globalLight = GetComponent<Light2D>();
        }
    }
    
    // GameManager에서 관리할 함수
    public void UpdateGlobalLight(Phase newPhase)
    {
        if (globalLight == null) return;
        
        if (lightCoroutine != null)
        {
            StopCoroutine(lightCoroutine);
        }

        float targetIntensity = (newPhase == Phase.Night) ? nightIntensity : dayIntensity;
        Color targetColor = (newPhase == Phase.Night) ? nightColor : dayColor;

        lightCoroutine = StartCoroutine(TransitionLight(targetIntensity, targetColor));
    }

    private IEnumerator TransitionLight(float targetIntensity, Color targetColor)
    {
        float timeElapsed = 0f;
        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;
        
        if (Mathf.Abs(startIntensity - targetIntensity) < 0.01f)
        {
            globalLight.intensity = targetIntensity;
            globalLight.color = targetColor;
            yield break;
        }

        while (timeElapsed < transitionDuration)
        {
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, timeElapsed / transitionDuration);
            globalLight.color = Color.Lerp(startColor, targetColor, timeElapsed / transitionDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        globalLight.intensity = targetIntensity;
        globalLight.color = targetColor;
        lightCoroutine = null;
    }
}