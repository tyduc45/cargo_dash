using UnityEngine;

public class BirdSounds : MonoBehaviour
{
    public AudioClip birdMove;


    public void PlaySound()
    {
        
        if (!SoundManager.Instance.audioSource.isPlaying)
        {
;
            SoundManager.Instance.PlayBirdSound(birdMove, 0.45f);
        }

    }
}
