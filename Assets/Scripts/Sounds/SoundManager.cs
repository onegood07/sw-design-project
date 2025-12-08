using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    // 1. 외부에서 접근해야 하므로 public으로 변경!
    public static SoundManager Instance; 
    [Range(0f,1f)]
    [SerializeField]private float defaultVolume;
    public AudioMixer audioMixer;
    
    [Header("효과음 재생기")]
    public AudioSource sfxSource; 

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        SetVolume(defaultVolume);
    }

    public void SetVolume(float volume)
    {
        if (volume <= 0) volume = 0.0001f; 
        
        // 믹서 볼륨 조절 (이게 전체 소리를 줄여줌)
        audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20); 
    }

    // volume 매개변수를 선택적으로 받을 수 있게 수정 (기본값 1.0f)
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return;

        // 2. v 변수 삭제. 믹서가 이미 소리를 줄였으므로 여기선 그냥 튼다.
        // volumeScale은 "이 효과음만 특별히 좀 작게/크게 틀고 싶을 때" 씀
        sfxSource.PlayOneShot(clip, volumeScale);
    }
}