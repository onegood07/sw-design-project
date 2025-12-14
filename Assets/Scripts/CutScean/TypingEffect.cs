using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TypingEffect : MonoBehaviour
{
    public Text targetText;
    public float typingSpeed = 0.05f; // 글자 간격

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip typingClip;

    // 외부에서 연결할 이벤트
    public System.Action onTypingFinished;


    Coroutine typingCoroutine;
    bool isTyping;
    string currentMessage;

    public bool IsTyping => isTyping;

    public void Play(string message)
    {
        // 이전 타이핑 중지
        StopTyping();

        currentMessage = message;
        typingCoroutine = StartCoroutine(TypeText(message));
    }

    public void Skip()
    {
        if (!isTyping) return;

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        targetText.text = currentMessage;
        StopTypingSound();

        isTyping = false;
        onTypingFinished?.Invoke();
    }

    IEnumerator TypeText(string message)
    {
        isTyping = true;
        targetText.text = "";
        
        StartTypingSound();

        foreach (char c in message)
        {
            targetText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        StopTypingSound();

        isTyping = false;
        typingCoroutine = null;
    }

    void StartTypingSound()
    {
        if (audioSource == null || typingClip == null) return;

        audioSource.clip = typingClip;
        audioSource.pitch = Random.Range(0.97f, 1.03f); // 자연스러움
        audioSource.Play();
    }

    void StopTypingSound()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    
    void StopTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        StopTypingSound();

        typingCoroutine = null;
        isTyping = false;
    }

        public void OnClick()
    {
        if (isTyping)
        {
            Skip();        // 타이핑 스킵
        }
        else
        {
            onTypingFinished?.Invoke(); // 다음 대사 요청
        }
    }


    void Update()
    {
        if (!isTyping) return;

        if (
            Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame
        )
        {
            OnClick();
        }
    }
}
