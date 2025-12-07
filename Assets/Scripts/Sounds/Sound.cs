using UnityEngine;

public class Sound : MonoBehaviour
{
    [SerializeField]private AudioClip c;
    public void soundOn()
    {
        SoundManager.Instance.PlaySFX(c,1.0f);
    }
}
