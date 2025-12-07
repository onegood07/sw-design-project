using UnityEngine;
using UnityEngine.Audio;

public class VolumeControler : MonoBehaviour
{
    public AudioMixer audioMixer; // 2. 믹서 연결할 변수
    public void SetVolume(float volume)
    {
        // 3. 슬라이더 값(0~1)을 데시벨(-80~0)로 변환
        // 로그(Log)를 쓰는 이유는 소리 크기가 지수함수적으로 들리기 때문입니다.
        // volume이 0이 되면 에러나니까 최소값을 0.0001로 하거나 예외처리하세요.
        
        if (volume <= 0) volume = 0.0001f; 
        
        // 믹서 이름을 Master 로 해두었음
        audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20); 
    }
}